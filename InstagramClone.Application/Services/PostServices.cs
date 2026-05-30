using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Results;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Common.Constants;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Domain.Enums;

using System.Text.RegularExpressions;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Helpers;
using Serilog;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace InstagramClone.Application.Services;
public class PostServices(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser, IStorageServices storageServices, ICacheService cache) : IPostServices
{
    public async Task<Result<string>> CreatePostAsync(CreatePostDto createPostDto)
    {
        // validate userId
        var userId = currentUser.UserId;

        // 2. create post
        var newPost = new Post
        {
            Content = createPostDto.Content ?? string.Empty,
            UserId = userId,
            MediaItems = new List<PostMedia>()
        };

        // lưu tạm url để rollback nếu có lỗi
        var uploadUrls = new List<string>();

        // 3. upload media files
        try
        {
            foreach(var file in createPostDto.Files)
            {
                var uploadResult = await storageServices.UploadImageAsync(file, userId, "posts", 1080, 1350);

                if(!uploadResult.IsSuccess)
                {
                    throw new Exception($"Failed to upload file: {file.FileName}");
                }

                // 
                var mediaUrl = uploadResult.Value!;
                uploadUrls.Add(mediaUrl);

                // add media url to post
                newPost.MediaItems.Add(new PostMedia { MediaUrl = mediaUrl });
            }

            // 4. save post to database
            unitOfWork.Posts.Add(newPost);

            var tags = ExtractHashtags(createPostDto.Content ?? string.Empty);

            if(tags.Any())
            {
                var exitstingTags = await unitOfWork.Hashtags.QueryNoTracking()
                    .Where(t => tags.Contains(t.Name))
                    .ToListAsync();
                
                foreach(var tag in tags)
                {
                    var hashtagEntity = exitstingTags.FirstOrDefault(t => t.Name == tag);

                    if(hashtagEntity == null)
                    {
                        hashtagEntity = new Hashtag { Name = tag };
                        unitOfWork.Hashtags.Add(hashtagEntity);
                    }

                    unitOfWork.PostHashtags.Add(new PostHashtag
                    {
                        Hashtag = hashtagEntity,
                        Post = newPost
                    });
                }
            }

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if(!saved)                    
            {
                Log.Error("User {UserId} failed to save post {Content} to database", userId, createPostDto.Content);
                throw new Exception("Failed to save post to database");
            }
                
            Log.Information("User {UserId} created post {PostId} with {MediaCount} images", userId, newPost.Id, newPost.MediaItems.Count);
            await cache.BumpScopeVersionAsync("posts:feed:data");
            await cache.BumpScopeVersionAsync("posts:search:data");
            await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
            return Result<string>.Success(newPost.Id.ToString());

        }
        catch (Exception ex) 
        {
            Log.Error(ex, "Error creating post for userId {UserId}: {Message}", userId, ex.Message);
            // rollback uploaded files
            foreach (var url in uploadUrls)
            {
                await storageServices.DeleteFile(url);
            }
                
            return Result<string>.Failure(new Error("PostCreationFailed", ex.Message));
        }
    }

    public async Task<Result> DeletePostAsync(Guid postId)
    {
        var userId = currentUser.UserId;
        if(string.IsNullOrEmpty(userId))
            return Result.Failure(new Error("Unauthorized", "User is not authenticated"));

        //1. find post end medias 
        var post = await unitOfWork.Posts.Query().Include(p => p.MediaItems).FirstOrDefaultAsync(p => p.Id == postId);

        if(post == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        //2. check ownership
        if(post.UserId != userId)
            return Result.Failure(new Error(ErrorCodes.Forbid, "User is not the owner of the post"));

        //3. soft delete post and medias
        post.IsDeleted = true; // soft delete post

        foreach (var media in post.MediaItems)
        {
            media.IsDeleted = true; // soft delete media
        }

        unitOfWork.Posts.Update(post);

        //4. save changes to database
        try
        {
            // Tách riêng ra để kiểm tra
            var rowsAffected = await unitOfWork.SaveChangesAsync();

            if (rowsAffected == 0)
            {
                Log.Warning("User {UserId} tried to delete post {PostId} but no rows were affected", userId, postId);
                return Result.Failure(new Error(ErrorCodes.Failure, "SaveChanges executed but no rows were affected in the Database!"));
            }

            Log.Information("User {UserId} deleted post {PostId}", userId, postId);
            await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
            await cache.BumpScopeVersionAsync("posts:feed:data");
            await cache.BumpScopeVersionAsync("posts:search:data");
            await cache.BumpScopeVersionAsync($"user:profile:rev:{post.UserId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting post {PostId} for user {UserId}", postId, userId);
            return Result.Failure(new Error(ErrorCodes.Failure, "An error occurred while deleting the post, please try again later."));
        }
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetFeedsAsync(CursorPaginationRequest cursorPagination)
    {
        var feedDataVer = await cache.GetScopeVersionAsync("posts:feed:data");
        var feedUserScope = await cache.GetScopeVersionAsync($"posts:feed:scope:{currentUser.UserId}");
        var cacheKey = $"posts:feed:{currentUser.UserId}:{feedDataVer}:{feedUserScope}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";

        var cachedFeed = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            var userId = currentUser.UserId;

            // Lấy danh sách ID những người mình đang Follow (đã Accepted)
            var followingIds = await unitOfWork.Follows.QueryNoTracking()
                .Where(f => f.ObserverId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.TargetId)
                .ToListAsync();

            followingIds.Add(userId);

            var query = unitOfWork.Posts.QueryNoTracking()
                .Where(p => followingIds.Contains(p.UserId));

            if (cursorPagination.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(cursorPagination.PageSize + 1)
                .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId = currentUser.UserId })
                .ToListAsync();

            return PaginationHelper.ToCursorPaged(posts, cursorPagination.PageSize, p => p.CreatedAt);
        });

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedFeed);
    }

    public async Task<Result<bool>> UpdatePostAsync(string content, Guid postId)
    {
        var userId = currentUser.UserId;
        await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
        // 1. Chỉ chọc xuống DB 1 lần duy nhất để lấy bài viết
        var postToUpdate = await unitOfWork.Posts.Query().FirstOrDefaultAsync(p => p.Id == postId);

        // 2. Chặn lỗi 1: Bài viết không tồn tại (Bị xóa hoặc truyền sai ID)
        if (postToUpdate == null)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Post does not exist."));
        }

        // 3. Chặn lỗi 2 (Bảo mật): Bài viết tồn tại nhưng KHÔNG thuộc về user đang đăng nhập
        if (postToUpdate.UserId != userId)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.Forbid, "You do not have permission to edit this post."));
        }

        // 4. Thực hiện cập nhật dữ liệu (Không cần dùng context.Posts.Update)
        postToUpdate.Content = content;

        try
        {
            var rowsAffected = await unitOfWork.SaveChangesAsync();

            // LƯU Ý UX QUAN TRỌNG: 
            // Nếu user bấm "Sửa" nhưng KHÔNG thay đổi chữ nào, EF Core sẽ phát hiện ra và không chạy lệnh SQL.
            // Lúc này rowsAffected = 0. Đây không phải là lỗi, nên ta vẫn trả về Success.

            await cache.BumpScopeVersionAsync("posts:feed:data");
            await cache.BumpScopeVersionAsync("posts:search:data");
            await cache.BumpScopeVersionAsync($"user:profile:rev:{postToUpdate.UserId}");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error in UpdatePostAsync] {ex}");
            return Result<bool>.Failure(new Error(ErrorCodes.Failure, "An error occurred while updating the post, please try again later."));
        }
    }
    public async Task<Result<ResponsePostDto>> GetPostByIdAsync(Guid postId)
    {
        var userId = currentUser.UserId;
        var detailRev = await cache.GetScopeVersionAsync($"post:detail:rev:{postId}");
        var cacheKey = $"post:detail:{postId}:{userId}:{detailRev}";

        var cachedPost = await cache.GetOrCreateAsync<ResponsePostDto?>(cacheKey, factory: async () => 
        {
            var post = await unitOfWork.Posts
            .QueryNoTracking()
            .Where(p => p.Id == postId)
            .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId = userId })
            .FirstOrDefaultAsync();
            if (post == null)
                return null;

            return post;
        });
        if(cachedPost == null)
            return Result<ResponsePostDto>.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        return Result<ResponsePostDto>.Success(cachedPost);
    }
    
    //public async Task<Result<List<ResponsePostDto>>> GetFeedAsync(CursorPaginationRequest cursorPagination)
    //{
    //    var userId = currentUser.UserId;
    //    // Bước 1: Lấy danh sách ID những người mình đang Follow (đã Accepted)
    //    var followingIds = await context.Follows
    //        .Where(f => f.ObserverId == userId && f.Status == FollowStatus.Accepted)
    //        .Select(f => f.TargetId)
    //        .ToListAsync();
    //    // Bước 2: Nhét luôn ID của chính mình vào danh sách này
    //    followingIds.Add(userId); // Thêm userId của chính mình để hiển thị cả bài viết của bản thân trong feed
    //    // Bước 3: Dựng Query gom chung (Người quen HOẶC Tài khoản Public)
    //    var query = context.Posts.AsNoTracking()
    //        .Where(p => followingIds.Contains(p.UserId) || p.User.IsPrivateAccount == false);

    //    // Bước 4: Chặn Cursor để chống trôi bài và tăng tốc độ query
    //    if(cursorPagination.Cursor.HasValue)
    //    {
    //        query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
    //    }
    //    // Bước 5: Lấy dữ liệu và Map ra DTO
    //    var feed = await query 
    //        .OrderByDescending(p => p.CreatedAt)
    //        .Take(cursorPagination.PageSize + 1)
    //        .Select(p => new ResponsePostDto
    //        {
    //            Id = p.Id,
    //            Content = p.Content,
    //            CreatedAt = p.CreatedAt,
    //            AuthorId = p.UserId,
    //            AuthorName = p.User.FullName,
    //            AuthorAvatar = p.User.AvatarUrl,
    //            MediaUrls = p.MediaItems.Select(m => m.MediaUrl).ToList(),
    //            LikeCount = p.Likes.Count(),
    //            IsLiked = p.Likes.Any(l => l.UserId == userId && !l.IsDeleted), // kiểm tra xem user hiện tại đã like bài post chưa
    //            CommentCount = p.Comments.Count()
    //        })
    //        .ToListAsync();
    //    return Result<List<ResponsePostDto>>.Success(feed);
    //}

    public async Task<Result> ToggleSavePostAsync(Guid postId)
    {
        var currentUserId = currentUser.UserId;

        var postExists = await unitOfWork.Posts.AnyAsync(p => p.Id == postId);
        if(!postExists)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        var existingSave = await unitOfWork.SavedPosts.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId && s.PostId == postId);

        if (existingSave == null)
        {
            var newSave = new SavedPost
            {
                UserId = currentUserId,
                PostId = postId
            };
            unitOfWork.SavedPosts.Add(newSave);
        }
        else
        {
            existingSave.IsDeleted = !existingSave.IsDeleted; // toggle trạng thái saved/unsaved
            unitOfWork.SavedPosts.Update(existingSave);
        }
        try
        {
            await unitOfWork.SaveChangesAsync();
            await cache.BumpScopeVersionAsync($"posts:feed:scope:{currentUserId}");
            await cache.BumpScopeVersionAsync($"posts:saved:scope:{currentUserId}");
            await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Toggle save failed for post {PostId} user {UserId}", postId, currentUserId);
            return Result.Failure(new Error(ErrorCodes.Failure, "Failed to update post save status."));
        }
    }
    
    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSavedPostsAsync(CursorPaginationRequest cursorPagination)
    {
        var userId = currentUser.UserId;
        var savedScope = await cache.GetScopeVersionAsync($"posts:saved:scope:{userId}");
        var cacheKey = $"posts:saved:{userId}:{savedScope}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";
        var cachedSavedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            var query = unitOfWork.SavedPosts
           .QueryNoTracking()
           .Where(s => s.UserId == userId && !s.IsDeleted)
           .Select(s => s.Post);
            if (cursorPagination.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
            }
            var savedPosts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(cursorPagination.PageSize + 1)
                .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId = userId })
                .ToListAsync();

            return PaginationHelper.ToCursorPaged(savedPosts, cursorPagination.PageSize, p => p.CreatedAt);
        });

       
        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedSavedPosts);
    }

    private List<string> ExtractHashtags(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();

        // \p{L}: Chữ cái (Bao gồm tiếng Việt)
        // \p{N}: Số
        // _: Dấu gạch dưới
        var regex = new Regex(@"#[\p{L}\p{N}_]+");

        return regex.Matches(content)
                    .Select(m => m.Value.ToLower()) // Ép về chữ thường hết (VD: #VNU và #vnu là một)
                    .Distinct()                     // Lọc trùng (Lỡ user gõ 2 chữ #vnu trong 1 bài)
                    .ToList();
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSearchPostsAsync(string content, CursorPaginationRequest request)
    {
        var currentUserId = currentUser.UserId;
        var searchDataVer = await cache.GetScopeVersionAsync("posts:search:data");
        var feedUserScope = await cache.GetScopeVersionAsync($"posts:feed:scope:{currentUser.UserId}");
        var cacheKey = $"posts:search:{content}:{currentUserId}:{searchDataVer}:{feedUserScope}:{request.PageSize}:{request.Cursor}";

        var cachedSearchResult = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // 1. CHUẨN HÓA NGAY TỪ ĐẦU (Cực kỳ quan trọng)
            content = content.Trim().ToLower();

            var query = unitOfWork.Posts.QueryNoTracking();

            if (content.StartsWith("#"))
            {
                query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name.ToLower() == content));
            }
            else
            {
                query = query.Where(p => EF.Functions.Like(p.Content, $"%{content}%"));
            }

            if (request.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < request.Cursor.Value);
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(request.PageSize + 1)
                .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId = currentUserId })
                .ToListAsync();

            return PaginationHelper.ToCursorPaged(posts, request.PageSize, p => p.CreatedAt);
        });

        if(cachedSearchResult == null)
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Search content cannot be empty"));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedSearchResult);
    }

    public async Task<Result> ToggleLikeAsync(Guid postId)
    {
        var userId = currentUser.UserId;
        await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
        await cache.BumpScopeVersionAsync($"posts:feed:scope:{userId}");
        // 1. Kiểm tra xem bài post có tồn tại không
        var existingPost = await unitOfWork.Posts.AnyAsync(p => p.Id == postId);
        if (!existingPost)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        // 2. Kiểm tra xem user đã like bài post chưa
        var existingLike = await unitOfWork.Likes.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        // IgnoreQueryFilters() để bỏ qua global filter IsDeleted, vì có thể user đã like rồi nhưng sau đó bị soft delete, nên vẫn phải kiểm tra để toggle lại

        if (existingLike == null)
        {
            var newLike = new Like
            {
                PostId = postId,
                UserId = userId
            };
            unitOfWork.Likes.Add(newLike);
        }
        else
        {
            existingLike.IsDeleted = !existingLike.IsDeleted; // toggle
            unitOfWork.Likes.Update(existingLike);
        }
        try
        {
            await unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Toggle like failed for post {PostId} user {UserId}", postId, userId);
            return Result.Failure(new Error(ErrorCodes.Failure, "Failed to update like status."));
        }
    }
}



