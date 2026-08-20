using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.RegularExpressions;

namespace InstagramClone.Application.Features.Posts.Services;

public class PostServices(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IStorageServices storageServices,
    ICacheService cache,
    IBackgroundJobService backgroundJobService,
    IOptions<MediaSettings> mediaSettingsOptions
    ) : IPostServices
{
    private readonly MediaSettings _mediaSettings = mediaSettingsOptions.Value;

    public async Task<Result<string>> CreatePostAsync(CreatePostDto createPostDto)
    {
        // validate userId
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        // 2. create post
        var newPost = new Post(userId, createPostDto.Content ?? string.Empty);

        // lưu tạm url để rollback nếu có lỗi
        var uploadUrls = new List<string>();

        // 3. upload media files
        try
        {
            foreach(var file in createPostDto.Files)
            {
                var uploadResult = await storageServices.UploadImageAsync(file, userId.ToString(), "posts", _mediaSettings.Post.MaxWidth, _mediaSettings.Post.MaxHeight);

                if(!uploadResult.IsSuccess)
                {
                    return Result<string>.Failure(new Error(ErrorCodes.Failure, $"Failed to upload media file {file.FileName}: {uploadResult.Errors.FirstOrDefault().Description}"));
                }

                var mediaUrl = uploadResult.Value!;
                uploadUrls.Add(mediaUrl);

                // add media url to post
                newPost.AddMedia(new PostMedia { MediaUrl = mediaUrl });
            }

            // 4. save post to database
            unitOfWork.Posts.Add(newPost);

            var tags = ExtractHashtags(createPostDto.Content ?? string.Empty);

            if(tags.Any())
            {
                var exitstingTags = await unitOfWork.Posts.GetHashtagsByNamesAsync(tags);
                
                foreach(var tag in tags)
                {
                    var hashtagEntity = exitstingTags.FirstOrDefault(t => t.Name == tag);

                    if(hashtagEntity == null)
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
            if(!saved)                    
            {
                Log.Error("User {UserId} failed to save post {Content} to database", userId, createPostDto.Content);
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to save post to database"));
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
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result.Failure(new Error("Unauthorized", "User is not authenticated"));

        //1. find post end medias 
        var post = await unitOfWork.Posts.GetByIdWithMediaAsync(postId);

        if(post == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        //2. check ownership or admin role
        if (post.UserId != userId && !currentUser.IsAdmin)
            return Result.Failure(new Error(ErrorCodes.Forbid, "User is not authorized to delete this post"));

        //3. soft delete post and medias
        post.MarkAsDeleted(); // soft delete post

        foreach (var media in post.MediaItems)
        {
            media.MarkAsDeleted(); // soft delete media
        }

        unitOfWork.Posts.Update(post);

        //4. save changes to database
        try
        {
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
            if (!Guid.TryParse(currentUser.UserId, out var userId)) throw new InvalidOperationException("Invalid user ID"); // This should not happen if the user is authenticated

            // Lấy danh sách ID những người mình đang Follow (đã Accepted)
            var followingIds = await unitOfWork.Users.GetFollowingIdsAsync(userId);
            followingIds.Add(userId);

            var posts = await unitOfWork.Posts.GetFeedsAsync(followingIds, cursorPagination.Cursor, cursorPagination.PageSize, userId);

            return PaginationHelper.ToCursorPaged(posts, cursorPagination.PageSize, p => p.CreatedAt);
        });

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedFeed);
    }

    public async Task<Result<bool>> UpdatePostAsync(string content, Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
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
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<ResponsePostDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        var detailRev = await cache.GetScopeVersionAsync($"post:detail:rev:{postId}");
        var cacheKey = $"post:detail:{postId}:{userId}:{detailRev}";

        var cachedPost = await cache.GetOrCreateAsync<ResponsePostDto?>(cacheKey, factory: async () => 
        {
            return await unitOfWork.Posts.GetPostDtoByIdAsync(postId, userId);
        });
        if(cachedPost == null)
            return Result<ResponsePostDto>.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        return Result<ResponsePostDto>.Success(cachedPost);
    }

    public async Task<Result> ToggleSavePostAsync(Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId)) return Result.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var postExists = await unitOfWork.Posts.AnyAsync(p => p.Id == postId);
        if(!postExists)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Post does not exist"));

        var existingSave = await unitOfWork.Posts.GetSavedPostAsync(currentUserId, postId, includeDeleted: true);

        if (existingSave == null)
        {
            var newSave = new SavedPost(currentUserId, postId);
            unitOfWork.Posts.AddSavedPost(newSave);
        }
        else
        {
            existingSave.ToggleDeleted(); // toggle trạng thái saved/unsaved
            unitOfWork.Posts.UpdateSavedPost(existingSave);
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
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        var savedScope = await cache.GetScopeVersionAsync($"posts:saved:scope:{userId}");
        var cacheKey = $"posts:saved:{userId}:{savedScope}:{cursorPagination.PageSize}:{cursorPagination.Cursor}";
        var cachedSavedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            var posts = await unitOfWork.Posts.GetSavedPostsAsync(userId, cursorPagination.Cursor, cursorPagination.PageSize);
            return PaginationHelper.ToCursorPaged(posts, cursorPagination.PageSize, p => p.CreatedAt);
        });

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
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId)) return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        var searchDataVer = await cache.GetScopeVersionAsync("posts:search:data");
        var feedUserScope = await cache.GetScopeVersionAsync($"posts:feed:scope:{currentUser.UserId}");
        var cacheKey = $"posts:search:{content}:{currentUserId}:{searchDataVer}:{feedUserScope}:{request.PageSize}:{request.Cursor}";

        var cachedSearchResult = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Search content cannot be empty");
            }

            var posts = await unitOfWork.Posts.GetSearchPostsAsync(content, request.Cursor, request.PageSize, currentUserId);
            return PaginationHelper.ToCursorPaged(posts, request.PageSize, p => p.CreatedAt);
        });

        if(cachedSearchResult == null)
            return Result<CursorPagedResponse<ResponsePostDto>>.Failure(new Error(ErrorCodes.BadRequest, "Search content cannot be empty"));

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedSearchResult);
    }

    public async Task<Result> ToggleLikeAsync(Guid postId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        await cache.BumpScopeVersionAsync($"post:detail:rev:{postId}");
        await cache.BumpScopeVersionAsync($"posts:feed:scope:{userId}");

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
            if (isNowLiked)
            {
                backgroundJobService.Enqueue<INotificationServices>(svc =>
                    svc.CreateAndSendNotificationAsync(post.UserId, userId, NotificationType.LikePost, "liked your post.", postId));
            }
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            Log.Error("Toggle like failed for post {PostId} user {UserId}", postId, userId);
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

        var profileRev = await cache.GetScopeVersionAsync($"user:profile:rev:{targetUserId}");
        var userScope = await cache.GetScopeVersionAsync($"posts:feed:scope:{currentUserId}");
        var cacheKey = $"posts:user:{targetUserId}:{currentUserId}:{profileRev}:{userScope}:{request.PageSize}:{request.Cursor}";

        var cachedPosts = await cache.GetOrCreateAsync<CursorPagedResponse<ResponsePostDto>>(cacheKey, factory: async () =>
        {
            var posts = await unitOfWork.Posts.GetUserPostsAsync(targetUserGuid, request.Cursor, request.PageSize, currentUserId);
            return PaginationHelper.ToCursorPaged(posts, request.PageSize, p => p.CreatedAt);
        });

        return Result<CursorPagedResponse<ResponsePostDto>>.Success(cachedPosts!);
    }
}
