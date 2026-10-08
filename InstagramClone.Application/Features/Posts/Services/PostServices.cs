using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Posts.Mappings;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Posts.Services;

public class PostServices(IUnitOfWork unitOfWork,
                          ICurrentUserService currentUser,
                          IStorageServices storageServices,
                          ICacheService cache,
                          IBackgroundJobService backgroundJobService,
                          Microsoft.Extensions.Options.IOptions<InstagramClone.Common.Models.Config.MediaSettings> mediaSettingsOptions) : IPostServices
{
    private readonly InstagramClone.Common.Models.Config.MediaSettings _mediaSettings = mediaSettingsOptions.Value;
    public async Task<Result<string>> CreatePostAsync(CreatePostDto createPostDto)
    {
        var uploadUrls = new List<string>();

        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result<string>.Failure(new Error("Unauthorized", "User is not authenticated"));

        try
        {
            var newPost = new Post(userId, createPostDto.Content ?? string.Empty);

            foreach (var file in createPostDto.Files)
            {
                var uploadResult = await storageServices.UploadImageAsync(file, userId.ToString(), "posts", _mediaSettings.Post.MaxWidth, _mediaSettings.Post.MaxHeight);

                if (!uploadResult.IsSuccess)
                {
                    return Result<string>.Failure(new Error(ErrorCodes.Failure, $"Failed to upload media file {file.FileName}: {uploadResult.Errors.FirstOrDefault().Description}"));
                }

                var mediaUrl = uploadResult.Value!;
                uploadUrls.Add(mediaUrl);

                newPost.AddMedia(new PostMedia { MediaUrl = mediaUrl });
            }

            unitOfWork.Posts.Add(newPost);

            var tags = ExtractHashtags(createPostDto.Content ?? string.Empty);

            if (tags.Any())
            {
                var existingTags = await unitOfWork.Posts.GetHashtagsByNamesAsync(tags);

                foreach (var tag in tags)
                {
                    var hashtagEntity = existingTags.FirstOrDefault(t => t.Name == tag);

                    if (hashtagEntity == null)
                    {
                        hashtagEntity = new Hashtag { Name = tag };
                        unitOfWork.Posts.AddHashtag(hashtagEntity);
                    }

                    unitOfWork.Posts.AddPostHashtag(new PostHashtag
                    {
                        Hashtag = hashtagEntity,
                        Post = newPost
                    });
                }
            }

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (!saved)
            {
                Log.Error("User {UserId} failed to save post {Content} to database", userId, createPostDto.Content);
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to save post to database"));
            }

            Log.Information("User {UserId} created post {PostId} with {MediaCount} images", userId, newPost.Id, newPost.MediaItems.Count);
            return Result<string>.Success(newPost.Id.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DB save failed after upload for userId {UserId}. Compensating: deleting {Count} uploaded file(s).", userId, uploadUrls.Count);

            // Compensating Transaction: xóa các file đã upload lên Cloud Storage để tránh file mồ côi
            await CleanupUploadedFilesAsync(uploadUrls);

            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to create post. Please try again."));
        }
    }

    public async Task<Result> DeletePostAsync(Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result.Failure(new Error("Unauthorized", "User is not authenticated"));

        var post = await unitOfWork.Posts.GetByIdWithMediaAsync(postId);

        if (post == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        if (post.UserId != userId && !currentUser.IsAdmin)
            return Result.Failure(new Error(ErrorCodes.Forbid, "User is not authorized to delete this post"));

        post.MarkAsDeleted();

        foreach (var media in post.MediaItems)
        {
            media.MarkAsDeleted();
        }

        unitOfWork.Posts.Update(post);

        try
        {
            var rowsAffected = await unitOfWork.SaveChangesAsync();

            if (rowsAffected == 0)
            {
                Log.Warning("User {UserId} tried to delete post {PostId} but no rows were affected", userId, postId);
                return Result.Failure(new Error(ErrorCodes.Failure, "SaveChanges executed but no rows were affected in the Database!"));
            }

            Log.Information("User {UserId} deleted post {PostId}", userId, postId);
            await cache.RemoveAsync($"post:detail:{postId}:{userId}");
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
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid user ID"));

        var cacheKey = $"posts:feed:{userId}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";

        var cachedFeed = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(
            cacheKey,
            factory: async () =>
            {
                var followingIds = await unitOfWork.Users.GetFollowingIdsAsync(userId);
                followingIds.Add(userId);

                var posts = await unitOfWork.Posts.GetFeedsAsync(followingIds, cursorPagination.Cursor, cursorPagination.PageSize);
                var postDtos = posts.ToResponsePostDtos(userId);

                return PaginationHelper.ToCursorPaged(postDtos, cursorPagination.PageSize, p => p.CreatedAt);
            },
            TimeSpan.FromSeconds(30));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedFeed);
    }

    public async Task<Result<bool>> UpdatePostAsync(string content, Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var postToUpdate = await unitOfWork.Posts.GetByIdAsync(postId);

        if (postToUpdate == null)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Post does not exist."));
        }

        if (postToUpdate.UserId != userId)
        {
            return Result<bool>.Failure(new Error(ErrorCodes.Forbid, "You do not have permission to edit this post."));
        }

        postToUpdate.EditContent(content);

        try
        {
            await unitOfWork.SaveChangesAsync();
            await cache.RemoveAsync($"post:detail:{postId}:{userId}");
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
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<ResponsePostDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var cacheKey = $"post:detail:{postId}:{userId}";

        var cachedPost = await cache.GetOrCreateAsync<ResponsePostDto?>(
            cacheKey, 
            factory: async () => 
            {
                var post = await unitOfWork.Posts.GetPostDetailsByIdAsync(postId);
                return post?.ToResponsePostDto(userId);
            },
            TimeSpan.FromMinutes(2));

        if (cachedPost == null)
            return Result<ResponsePostDto>.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        return Result<ResponsePostDto>.Success(cachedPost);
    }

    public async Task<Result> ToggleSavePostAsync(Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId)) return Result.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var postExists = await unitOfWork.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        var existingSave = await unitOfWork.Posts.GetSavedPostAsync(currentUserId, postId, includeDeleted: true);

        if (existingSave == null)
        {
            var newSave = new SavedPost(currentUserId, postId);
            unitOfWork.Posts.AddSavedPost(newSave);
        }
        else
        {
            existingSave.ToggleDeleted();
            unitOfWork.Posts.UpdateSavedPost(existingSave);
        }
        try
        {
            await unitOfWork.SaveChangesAsync();
            await cache.RemoveAsync($"post:detail:{postId}:{currentUserId}");
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
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var cacheKey = $"posts:saved:{userId}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";
        var cachedSavedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(
            cacheKey, 
            factory: async () =>
            {
                var posts = await unitOfWork.Posts.GetSavedPostsAsync(userId, cursorPagination.Cursor, cursorPagination.PageSize);
                var dtos = posts.ToResponsePostDtos(userId);
                return PaginationHelper.ToCursorPaged(dtos, cursorPagination.PageSize, p => p.CreatedAt);
            },
            TimeSpan.FromSeconds(30));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedSavedPosts);
    }

    private List<string> ExtractHashtags(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();

        var regex = new Regex(@"#[\p{L}\p{N}_]+");

        return regex.Matches(content)
                    .Select(m => m.Value.ToLower())
                    .Distinct()
                    .ToList();
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSearchPostsAsync(string content, CursorPaginationRequest request)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId)) 
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Search content cannot be empty"));
        }

        var cacheKey = $"posts:search:{content.Trim().ToLower()}:{currentUserId}:{request.PageSize}:{request.Cursor}";

        var cachedSearchResult = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(
            cacheKey, 
            factory: async () =>
            {
                var posts = await unitOfWork.Posts.GetSearchPostsAsync(content, request.Cursor, request.PageSize);
                var dtos = posts.ToResponsePostDtos(currentUserId);
                return PaginationHelper.ToCursorPaged(dtos, request.PageSize, p => p.CreatedAt);
            },
            TimeSpan.FromSeconds(30));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedSearchResult!);
    }

    public async Task<Result> ToggleLikeAsync(Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var post = await unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        var existingLike = await unitOfWork.Posts.GetLikeAsync(userId, postId, includeDeleted: true);
        bool isNowLiked = false;

        if (existingLike == null)
        {
            var newLike = new Like(userId, postId);
            unitOfWork.Posts.AddLike(newLike);
            isNowLiked = true;
        }
        else
        {
            existingLike.ToggleDeleted();
            unitOfWork.Posts.UpdateLike(existingLike);
            isNowLiked = !existingLike.IsDeleted;
        }
        try
        {
            await unitOfWork.SaveChangesAsync();
            await cache.RemoveAsync($"post:detail:{postId}:{userId}");

            if (isNowLiked)
            {
                backgroundJobService.Enqueue<INotificationServices>(svc =>
                    svc.CreateAndSendNotificationAsync(post.UserId, userId, NotificationType.LikePost, "liked your post.", postId));
            }
            return Result.Success();
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            // Race condition: 2 request cùng Like đồng thời → DB reject request thứ 2 do UNIQUE constraint.
            // Đây là hành vi đúng của hệ thống (idempotent). Log Warning thay vì Error.
            Log.Warning("Race condition detected on Like: PostId={PostId}, UserId={UserId}. Treating as success (idempotent).", postId, userId);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Toggle like failed for post {PostId} user {UserId}", postId, userId);
            return Result.Failure(new Error(ErrorCodes.Failure, "Failed to update like status."));
        }
    }

    public async Task<Result<CursorPagedResponse<ResponsePostDto>>> GetUserPostsAsync(string targetUserId, CursorPaginationRequest request)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        if (!Guid.TryParse(targetUserId, out var targetUserGuid))
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid Target User ID"));

        var targetUser = await unitOfWork.Users.GetByIdAsync(targetUserGuid);
        if (targetUser == null)
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

        bool isMyAccount = currentUserId == targetUserGuid;
        if (!isMyAccount && targetUser.IsPrivateAccount)
        {
            var follow = await unitOfWork.Users.GetFollowAsync(currentUserId, targetUserGuid);
            if (follow == null || follow.Status != Domain.Enums.FollowStatus.Accepted)
            {
                return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.Forbid, "This account is private."));
            }
        }

        var cacheKey = $"posts:user:{targetUserId}:{currentUserId}:{request.PageSize}:{request.Cursor}";

        var cachedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(
            cacheKey, 
            factory: async () =>
            {
                var posts = await unitOfWork.Posts.GetUserPostsAsync(targetUserGuid, request.Cursor, request.PageSize);
                var dtos = posts.ToResponsePostDtos(currentUserId);
                return PaginationHelper.ToCursorPaged(dtos, request.PageSize, p => p.CreatedAt);
            },
            TimeSpan.FromSeconds(30));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedPosts!);
    }

    /// <summary>
    /// Compensating Transaction helper: Xóa các file đã upload khi DB save thất bại.
    /// Mỗi file được xóa trong try-catch riêng để tránh lỗi một file làm hỏng toàn bộ cleanup.
    /// </summary>
    private async Task CleanupUploadedFilesAsync(List<string> urls)
    {
        foreach (var url in urls)
        {
            try
            {
                await storageServices.DeleteFile(url);
                Log.Information("Cleaned up orphaned file: {Url}", url);
            }
            catch (Exception ex)
            {
                // Không throw — ghi log để xử lý thủ công sau nếu cần
                Log.Warning(ex, "Failed to cleanup orphaned file: {Url}. Manual cleanup may be required.", url);
            }
        }
    }

    /// <summary>
    /// Kiểm tra DbUpdateException có phải do UNIQUE constraint violation (race condition) không.
    /// SQL Server error 2627: Unique constraint. Error 2601: Unique index.
    /// PostgreSQL error 23505. SQLite error 19.
    /// </summary>
    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? string.Empty;
        return innerMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || innerMessage.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || innerMessage.Contains("2627", StringComparison.OrdinalIgnoreCase)
            || innerMessage.Contains("2601", StringComparison.OrdinalIgnoreCase);
    }
}
