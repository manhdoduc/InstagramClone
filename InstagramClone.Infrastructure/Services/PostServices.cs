using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Results;
using InstagramClone.Infrastructure.Data;
using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Common.Constants;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace InstagramClone.Infrastructure.Services;
public class PostServices(AppDbContext context, ICurrentUserService currentUser, IStorageServices storageServices) : IPostServices
{
    public async Task<Result<string>> CreatePostAsync(CreatePostDto createPostDto)
    {
        // validate userId
        var userId = currentUser.UserId;

        // 2. create post
        var newPost = new Post
        {
            Content = createPostDto.Content,
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

            var tags = ExtractHashtags(createPostDto.Content);

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
                throw new Exception("Failed to save post to database");
                
            return Result<string>.Success(newPost.Id.ToString());

        }
        catch (Exception ex) 
        {
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
                return Result.Failure(new Error(ErrorCodes.Failure, "Lệnh SaveChanges chạy nhưng không có dòng nào bị ảnh hưởng trong Database!"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Moi tận gốc cái InnerException ra để xem SQL chửi gì
            var rootError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Result.Failure(new Error(ErrorCodes.Failure, $"Lỗi SQL thật sự là: {rootError}"));
        }
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetPostsAsync(CursorPaginationRequest cursorPagination)
    {
        try
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
            return Result<CursorPagedResponse<ResponsePostDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.Failure, $"Lỗi khi truy vấn bài post: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> UpdatePostAsync(string content, Guid id)
    {
        var userId = currentUser.UserId;

        // 1. Chỉ chọc xuống DB 1 lần duy nhất để lấy bài viết
        var postToUpdate = await context.Posts.FirstOrDefaultAsync(p => p.Id == id);

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

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var rootError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Result<bool>.Failure(new Error(ErrorCodes.Failure, $"Lỗi SQL thật sự là: {rootError}"));
        }
    }
    public async Task<Result<ResponsePostDto>> GetPostByIdAsync(Guid postId)
    {
        var userId = currentUser.UserId;

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
                CommentCount = p.Comments.Count()
            })
            .FirstOrDefaultAsync();
        if(post == null)
            return Result<ResponsePostDto>.Failure(new Error(ErrorCodes.Failure, "Bài viết không tồn tại"));

        return Result<ResponsePostDto>.Success(post);
    }
    
    public async Task<Result<List<ResponsePostDto>>> GetFeedAsync(CursorPaginationRequest cursorPagination)
    {
        var userId = currentUser.UserId;
        // Bước 1: Lấy danh sách ID những người mình đang Follow (đã Accepted)
        var followingIds = await context.Follows
            .Where(f => f.ObserverId == userId && f.Status == FollowStatus.Accepted)
            .Select(f => f.TargetId)
            .ToListAsync();
        // Bước 2: Nhét luôn ID của chính mình vào danh sách này
        followingIds.Add(userId); // Thêm userId của chính mình để hiển thị cả bài viết của bản thân trong feed
        // Bước 3: Dựng Query gom chung (Người quen HOẶC Tài khoản Public)
        var query = context.Posts.AsNoTracking()
            .Where(p => followingIds.Contains(p.UserId) || p.User.IsPrivateAccount == false);

        // Bước 4: Chặn Cursor để chống trôi bài và tăng tốc độ query
        if(cursorPagination.Cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursorPagination.Cursor.Value);
        }
        // Bước 5: Lấy dữ liệu và Map ra DTO
        var feed = await query 
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
                CommentCount = p.Comments.Count()
            })
            .ToListAsync();
        return Result<List<ResponsePostDto>>.Success(feed);
    }

    public async Task<Result<string>> ToggleSavePostAsync(Guid postId)
    {
        var currentUserId = currentUser.UserId;

        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if(!postExists)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "Bài viết không tồn tại"));

        var existingSave = await context.SavedPosts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId && s.PostId == postId);

        string status = "";
        if (existingSave == null)
        {
            var newSave = new SavedPost
            {
                UserId = currentUserId,
                PostId = postId
            };
            context.SavedPosts.Add(newSave);
            status = SavePostStatus.Saved;
        }
        else
        {
            existingSave.IsDeleted = !existingSave.IsDeleted; // toggle trạng thái saved/unsaved
            status = existingSave.IsDeleted ? SavePostStatus.Unsaved : SavePostStatus.Saved;
        }
        await context.SaveChangesAsync();
        return Result<string>.Success(status);
    }
    
    public async Task<Result<List<ResponsePostDto>>> GetSavedPostsAsync(CursorPaginationRequest cursorPagination)
    {
        var userId = currentUser.UserId;
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
                CommentCount = p.Comments.Count()
            })
            .ToListAsync();
        return Result<List<ResponsePostDto>>.Success(savedPosts);
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

    public async Task<Result<List<ResponsePostDto>>> GetPostsByHashtagAsync(string tag, CursorPaginationRequest request)
    {
        var currentUserId = currentUser.UserId;

        if(string.IsNullOrWhiteSpace(tag))
        {
            return Result<List<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Hashtag không được để trống"));
        }

        // 1. Chuẩn hóa đầu vào (Đề phòng Frontend gửi "VNU" thay vì "#vnu")
        var formattedTag = tag.StartsWith("#") ? tag.ToLower() : "#" + tag.ToLower();

        // 2. Dựng Query: Xuyên qua bảng trung gian để tìm Post
        var query = context.Posts
            .AsNoTracking()
            // Giải thích: "Lấy những bài viết mà trong danh sách Hashtag của nó, có tồn tại CÁI NÀO ĐÓ mang tên này"
            .Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name == formattedTag));

        // 3. Phân trang bằng Cursor (Chống trôi bài giống y hệt News Feed)
        if (request.Cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < request.Cursor.Value);
        }

        // 4. Lấy dữ liệu và Map sang DTO
        var posts = await query
            .OrderByDescending(p => p.CreatedAt) // Mới nhất lên đầu
            .Take(request.PageSize)              // Lấy đúng số lượng 1 trang
            .Select(p => new ResponsePostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.UserId,
                AuthorName = p.User.FullName, // Hoặc FullName tùy ý bạn
                AuthorAvatar = p.User.AvatarUrl,
                MediaUrls = p.MediaPorts.Select(m => m.MediaUrl).ToList(),
                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(),
                IsLiked = !string.IsNullOrEmpty(currentUserId) && p.Likes.Any(l => l.UserId == currentUserId)
            })
            .ToListAsync();

        return Result<List<ResponsePostDto>>.Success(posts);
    }

    public async Task<Result<string>> ToggleLikeAsync(Guid postId)
    {
        var userId = currentUser.UserId;

        // 1. Kiểm tra xem bài post có tồn tại không
        var existingPost = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!existingPost)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        // 2. Kiểm tra xem user đã like bài post chưa
        var existingLike = await context.Likes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        // IgnoreQueryFilters() để bỏ qua global filter IsDeleted, vì có thể user đã like rồi nhưng sau đó bị soft delete, nên vẫn phải kiểm tra để toggle lại

        string actionResult = "";
        if (existingLike == null)
        {
            var newLike = new Like
            {
                PostId = postId,
                UserId = userId
            };
            context.Likes.Add(newLike);
            actionResult = LikeCodess.Liked;
        }
        else
        {
            existingLike.IsDeleted = !existingLike.IsDeleted; // nếu đã like rồi thì toggle lại (like -> unlike, unlike -> like)
            context.Likes.Update(existingLike);
            actionResult = existingLike.IsDeleted ? LikeCodess.Unlike : LikeCodess.Liked; // nếu sau khi toggle mà IsDeleted = true thì là unlike, ngược lại là like
        }
        var saved = await context.SaveChangesAsync() > 0;
        if (!saved)
            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to toggle like"));
        return Result<string>.Success(actionResult);
    }
}



