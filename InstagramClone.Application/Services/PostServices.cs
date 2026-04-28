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
using Serilog;

namespace InstagramClone.Application.Services;
public class PostServices(IApplicationDbContext context, ICurrentUserService currentUser, IStorageServices storageServices, ICacheService cache) : IPostServices
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
            MediaPorts = new List<PostMedia>()
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
                newPost.MediaPorts.Add(new PostMedia { MediaUrl = mediaUrl });
            }

            // 4. save post to database
            context.Posts.Add(newPost);

            var tags = ExtractHashtags(createPostDto.Content ?? string.Empty);

            if(tags.Any())
            {
                var exitstingTags = await context.Hashtags
                    .Where(t => tags.Contains(t.Name))
                    .ToListAsync();
                
                foreach(var tag in tags)
                {
                    var hashtagEntity = exitstingTags.FirstOrDefault(t => t.Name == tag);

                    if(hashtagEntity == null)
                    {
                        hashtagEntity = new Hashtag { Name = tag };
                        context.Hashtags.Add(hashtagEntity);
                    }

                    context.PostHashtags.Add(new PostHashtag
                    {
                        Hashtag = hashtagEntity,
                        Post = newPost
                    });
                }
            }

            var saved = await context.SaveChangesAsync() > 0;
            if(!saved)                    
            {
                Log.Error("User {UserId} failed to save post {Content} to database", userId, createPostDto.Content);
                throw new Exception("Failed to save post to database");
            }
                
            Log.Information("User {UserId} created post {PostId} with {MediaCount} images", userId, newPost.Id, newPost.MediaPorts.Count);
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
        var post = await context.Posts.Include(p => p.MediaPorts).FirstOrDefaultAsync(p => p.Id == postId);

        if(post == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        //2. check ownership
        if(post.UserId != userId)
            return Result.Failure(new Error(ErrorCodes.Forbid, "User is not the owner of the post"));

        //3. soft delete post and medias
        post.IsDeleted = true; // soft delete post

        foreach (var media in post.MediaPorts)
        {
            media.IsDeleted = true; // soft delete media
        }

        context.Posts.Update(post);

        //4. save changes to database
        try
        {
            // Tách riêng ra để kiểm tra
            var rowsAffected = await context.SaveChangesAsync();

            if (rowsAffected == 0)
            {
                Log.Warning("User {UserId} tried to delete post {PostId} but no rows were affected", userId, postId);
                return Result.Failure(new Error(ErrorCodes.Failure, "Lệnh SaveChanges chạy nhưng không có dòng nào bị ảnh hưởng trong Database!"));
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
            return Result.Failure(new Error(ErrorCodes.Failure, "Đã xảy ra lỗi khi xóa bài viết, vui lòng thử lại sau."));
        }
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetFeedsAsync(CursorPaginationRequest cursorPagination)
    {
        var feedDataVer = await cache.GetScopeVersionAsync("posts:feed:data");
        var feedUserScope = await cache.GetScopeVersionAsync($"posts:feed:scope:{currentUser.UserId}");
        var cacheKey = $"posts:feed:{currentUser.UserId}:{feedDataVer}:{feedUserScope}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";

        var cachedFeed = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            // 1. khởi tạo truy vấn cơ bản, chưa query dữ liệu
            var query = context.Posts.AsNoTracking();

            // 2. áp dụng phân trang cursor-based nếu có cursor
            // Nếu cursor có giá trị, chỉ lấy những bài post được tạo trước thời điểm cursor
            // tạo sau thì giá trị lớn hơn
            if (cursorPagination.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
            }

            // 3. lấy pageSize + 1 bản ghi để kiểm tra xem có trang tiếp theo hay không
            var posts = await query
                .OrderByDescending(p => p.CreatedAt) // sắp xếp theo createdAt giảm dần để lấy bài mới nhất trước
                .Take(cursorPagination.PageSize + 1)
                .Select(p => new ResponsePostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.UserId,
                    AuthorName = p.User.FullName,
                    AuthorAvatar = p.User.AvatarUrl,
                    MediaUrls = p.MediaPorts.Where(m => !m.IsDeleted).Select(m => m.MediaUrl).ToList(),

                    LikeCount = p.Likes.Count(),
                    IsLiked = p.Likes.Any(l => l.UserId == currentUser.UserId && !l.IsDeleted), // kiểm tra xem user hiện tại đã like bài post chưa
                    IsSaved = context.SavedPosts.Any(s => s.PostId == p.Id && s.UserId == currentUser.UserId),

                    CommentCount = p.Comments.Count()
                })
                .ToListAsync();

            // 4. xác định xem có trang tiếp theo hay không
            var hasNextPage = posts.Count > cursorPagination.PageSize; // true nếu có nhiều hơn pageSize bản ghi, tức là còn bản ghi cho trang tiếp theo
            DateTime? nextCursor = null;

            // 5. nếu có trang tiếp theo, lấy createdAt của bản ghi cuối cùng làm cursor cho trang tiếp theo
            if (hasNextPage)
            {
                posts.RemoveAt(cursorPagination.PageSize); // loại bỏ bản ghi thứ pageSize + 1 khỏi kết quả trả về 
                nextCursor = posts.Last().CreatedAt; // lấy createdAt của bản ghi cuối cùng làm cursor cho trang tiếp theo
            }

            var pagedResponse = new CursorPagedResponse<ResponsePostDto>
            {
                Items = posts,
                NextCursor = nextCursor,
                HasNextPage = hasNextPage
            };
            return pagedResponse;
        });

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedFeed);
    }

    public async Task<Result<bool>> UpdatePostAsync(string content, Guid postId)
    {
        var userId = currentUser.UserId;
        await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
        // 1. Chỉ chọc xuống DB 1 lần duy nhất để lấy bài viết
        var postToUpdate = await context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        // 2. Chặn lỗi 1: Bài viết không tồn tại (Bị xóa hoặc truyền sai ID)
        if (postToUpdate == null)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Bài viết không tồn tại."));
        }

        // 3. Chặn lỗi 2 (Bảo mật): Bài viết tồn tại nhưng KHÔNG thuộc về user đang đăng nhập
        if (postToUpdate.UserId != userId)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.Forbid, "Bạn không có quyền chỉnh sửa bài viết này."));
        }

        // 4. Thực hiện cập nhật dữ liệu (Không cần dùng context.Posts.Update)
        postToUpdate.Content = content;

        try
        {
            var rowsAffected = await context.SaveChangesAsync();

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
            return Result<bool>.Failure(new Error(ErrorCodes.Failure, "Đã xảy ra lỗi khi cập nhật bài viết, vui lòng thử lại sau."));
        }
    }
    public async Task<Result<ResponsePostDto>> GetPostByIdAsync(Guid postId)
    {
        var userId = currentUser.UserId;
        var detailRev = await cache.GetScopeVersionAsync($"post:detail:rev:{postId}");
        var cacheKey = $"post:detail:{postId}:{userId}:{detailRev}";

        var cachedPost = await cache.GetOrCreateAsync<ResponsePostDto?>(cacheKey, factory: async () => 
        {
            var post = await context.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new ResponsePostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.UserId,
                AuthorName = p.User.FullName,
                AuthorAvatar = p.User.AvatarUrl,
                MediaUrls = p.MediaPorts.Select(m => m.MediaUrl).ToList(),
                LikeCount = p.Likes.Count(),
                IsLiked = p.Likes.Any(l => l.UserId == userId && !l.IsDeleted), // kiểm tra xem user hiện tại đã like bài post chưa
                IsSaved = context.SavedPosts.Any(s => s.PostId == p.Id && s.UserId == userId),
                CommentCount = p.Comments.Count()
            })
            .FirstOrDefaultAsync();
            if (post == null)
                return null;

            return post;
        });
        if(cachedPost == null)
            return Result<ResponsePostDto>.NotFound(new Error(ErrorCodes.NotFound, "Bài viết không tồn tại"));

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
    //            MediaUrls = p.MediaPorts.Select(m => m.MediaUrl).ToList(),
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

        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if(!postExists)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Bài viết không tồn tại"));

        var existingSave = await context.SavedPosts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId && s.PostId == postId);

        if (existingSave == null)
        {
            var newSave = new SavedPost
            {
                UserId = currentUserId,
                PostId = postId
            };
            context.SavedPosts.Add(newSave);
        }
        else
        {
            existingSave.IsDeleted = !existingSave.IsDeleted; // toggle trạng thái saved/unsaved
            context.SavedPosts.Update(existingSave);
        }
        try
        {
            await context.SaveChangesAsync();
            await cache.BumpScopeVersionAsync($"posts:feed:scope:{currentUserId}");
            await cache.BumpScopeVersionAsync($"posts:saved:scope:{currentUserId}");
            await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Toggle save failed for post {PostId} user {UserId}", postId, currentUserId);
            return Result.Failure(new Error(ErrorCodes.Failure, "Không cập nhật được trạng thái lưu bài."));
        }
    }
    
    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSavedPostsAsync(CursorPaginationRequest cursorPagination)
    {
        var userId = currentUser.UserId;
        var savedScope = await cache.GetScopeVersionAsync($"posts:saved:scope:{userId}");
        var cacheKey = $"posts:saved:{userId}:{savedScope}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";
        var cachedSavedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            var query = context.SavedPosts
           .AsNoTracking()
           .Where(s => s.UserId == userId && !s.IsDeleted)
           .Select(s => s.Post);
            if (cursorPagination.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
            }
            var savedPosts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(cursorPagination.PageSize + 1)
                .Select(p => new ResponsePostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.UserId,
                    AuthorName = p.User.FullName,
                    AuthorAvatar = p.User.AvatarUrl,
                    MediaUrls = p.MediaPorts.Select(m => m.MediaUrl).ToList(),
                    LikeCount = p.Likes.Count(),
                    IsLiked = p.Likes.Any(l => l.UserId == userId && !l.IsDeleted), // kiểm tra xem user hiện tại đã like bài post chưa
                    IsSaved = true,
                    CommentCount = p.Comments.Count()
                })
                .ToListAsync();

            var hasNextPage = savedPosts.Count > cursorPagination.PageSize; // true nếu có nhiều hơn pageSize bản ghi, tức là còn bản ghi cho trang tiếp theo
            DateTime? nextCursor = null;

            // 5. nếu có trang tiếp theo, lấy createdAt của bản ghi cuối cùng làm cursor cho trang tiếp theo
            if (hasNextPage)
            {
                savedPosts.RemoveAt(cursorPagination.PageSize); // loại bỏ bản ghi thứ pageSize + 1 khỏi kết quả trả về 
                nextCursor = savedPosts.Last().CreatedAt; // lấy createdAt của bản ghi cuối cùng làm cursor cho trang tiếp theo
            }

            var pagedResponse = new CursorPagedResponse<ResponsePostDto>
            {
                Items = savedPosts,
                NextCursor = nextCursor,
                HasNextPage = hasNextPage
            };
            return pagedResponse;
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

            var query = context.Posts.AsNoTracking();

            // 2. PHÂN LOẠI TÌM KIẾM
            if (content.StartsWith("#"))
            {
                // Nhánh tìm theo Hashtag
                query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name.ToLower() == content));
            }
            else
            {
                // Nhánh tìm theo Nội dung Text
                query = query.Where(p => EF.Functions.Like(p.Content, $"%{content}%"));

                //dùng sql server full-text search để tìm kiếm chính xác hơn, nhưng cần cấu hình full-text index trên cột Content của bảng Posts trong database
                //query = query.Where(p => EF.Functions.Contains(p.Content, content));
            }

            // 3. Phân trang bằng Cursor
            if (request.Cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < request.Cursor.Value);
            }

            // 4. Lấy dữ liệu và Map sang DTO
            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(p => new ResponsePostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.UserId,
                    AuthorName = p.User.FullName,
                    AuthorAvatar = p.User.AvatarUrl,
                    MediaUrls = p.MediaPorts.Select(m => m.MediaUrl).ToList(),
                    LikeCount = p.Likes.Count(),
                    CommentCount = p.Comments.Count(),
                    IsLiked = !string.IsNullOrEmpty(currentUserId) && p.Likes.Any(l => l.UserId == currentUserId),
                    IsSaved = !string.IsNullOrEmpty(currentUserId) && context.SavedPosts.Any(s => s.PostId == p.Id && s.UserId == currentUserId)
                })
                .ToListAsync();

            var hasNextPage = posts.Count > request.PageSize;
            DateTime? nextCursor = null;

            if(hasNextPage)
            {
                posts.RemoveAt(request.PageSize);
                nextCursor = posts.Last().CreatedAt;
            }

            var pagedResponse = new CursorPagedResponse<ResponsePostDto>
            {
                Items = posts.Take(request.PageSize).ToList(),
                NextCursor = nextCursor,
                HasNextPage = hasNextPage
            };
            return pagedResponse;
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
        var existingPost = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!existingPost)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        // 2. Kiểm tra xem user đã like bài post chưa
        var existingLike = await context.Likes
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
            context.Likes.Add(newLike);
        }
        else
        {
            existingLike.IsDeleted = !existingLike.IsDeleted; // toggle
            context.Likes.Update(existingLike);
        }
        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Toggle like failed for post {PostId} user {UserId}", postId, userId);
            return Result.Failure(new Error(ErrorCodes.Failure, "Không cập nhật được trạng thái thích."));
        }
    }
}



