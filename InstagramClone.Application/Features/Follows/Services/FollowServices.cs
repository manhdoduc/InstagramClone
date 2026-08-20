using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using AutoMapper;
using System;
using System.Threading.Tasks;

using InstagramClone.Application.Features.Notifications.Services;

namespace InstagramClone.Application.Features.Follows.Services
{
    public class FollowServices(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICacheService cache,
        IBackgroundJobService backgroundJobService
        ) : IFollowService
    {
        public async Task<Result<string>> SendFollowRequestAsync(string followeeIdStr)
        {
            var followerId = currentUser.UserId;
            // Invalidate follow lists cached per (target, viewer)
            await cache.RemoveAsync($"followers:{followeeIdStr}:{followerId}");
            await cache.RemoveAsync($"following:{followerId}:{followeeIdStr}");

            if (!Guid.TryParse(followerId, out var followerGuid) || !Guid.TryParse(followeeIdStr, out var followeeGuid))
                return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid ID"));

            if (followerGuid == followeeGuid)
                return Result<string>.BadRequest(new Error(ErrorCodes.Conflict, "You cannot follow yourself."));

            var targetUser = await unitOfWork.Users.GetByIdAsync(followeeGuid);
            if (targetUser == null)
                return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "The user you are trying to follow does not exist."));

            var existingFollow = await unitOfWork.Users.GetFollowAsync(followerGuid, followeeGuid, includeDeleted: true);

            string message = "";

            var initialStatus = targetUser.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;
            bool shouldNotify = false;
            NotificationType notifyType = NotificationType.Follow;

            if (existingFollow == null)
            {
                unitOfWork.Users.AddFollow(new Follow(followerGuid, followeeGuid, initialStatus));
                message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                shouldNotify = true;
                notifyType = targetUser.IsPrivateAccount ? NotificationType.FollowRequest : NotificationType.Follow;
            }
            else
            {
                // kịch bản toggle follow/unfollow
                if (existingFollow.IsDeleted)
                {
                    existingFollow.Restore();
                    existingFollow.UpdateStatus(initialStatus);
                    unitOfWork.Users.UpdateFollow(existingFollow);
                    message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                    shouldNotify = true;
                    notifyType = targetUser.IsPrivateAccount ? NotificationType.FollowRequest : NotificationType.Follow;
                }
                else
                {
                    existingFollow.MarkAsDeleted();
                    unitOfWork.Users.UpdateFollow(existingFollow);
                    message = FollowCodes.CancelledFollowRequest;
                }
            }
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                // Profile cache: isFollowing/isRequested + follower/following counts depend on follow relation
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followeeIdStr}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followerId}");

                if (shouldNotify)
                {
                    string notifyMsg = notifyType == NotificationType.FollowRequest
                        ? "requested to follow you."
                        : "started following you.";
                    backgroundJobService.Enqueue<INotificationServices>(svc =>
                        svc.CreateAndSendNotificationAsync(followeeGuid, followerGuid, notifyType, notifyMsg, null));
                }
            }
            return Result<string>.Success(message);
        }

        public async Task<Result<bool>> AcceptFollowRequestAsync(string followerIdStr)
        {
            var userIdStr = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userIdStr}:{followerIdStr}");
            await cache.RemoveAsync($"following:{followerIdStr}:{followerIdStr}");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(followerIdStr, out var followerId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to accept follow requests."));

            var request = await unitOfWork.Users.GetPendingFollowRequestAsync(followerId, userId);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.UpdateStatus(FollowStatus.Accepted);
            unitOfWork.Users.UpdateFollow(request);

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followerId}");

                backgroundJobService.Enqueue<INotificationServices>(svc =>
                    svc.CreateAndSendNotificationAsync(followerId, userId, NotificationType.FollowAccept, "accepted your follow request.", null));
            }
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to accept follow request."));
        }

        public async Task<Result<bool>> DeclineFollowRequestAsync(string followerIdStr)
        {
            var userIdStr = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userIdStr}:{followerIdStr}");
            await cache.RemoveAsync($"following:{followerIdStr}:{followerIdStr}");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(followerIdStr, out var followerId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to decline follow requests."));

            var request = await unitOfWork.Users.GetPendingFollowRequestAsync(followerId, userId);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.MarkAsDeleted();
            unitOfWork.Users.UpdateFollow(request);

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followerId}");
            }
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to decline follow request."));
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowerAsync(string targetUserIdStr, CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId))
                return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var cacheKey = $"followers:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowers = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                return await unitOfWork.Users.GetFollowersAsync(targetUserId, currentUserId, request);
            });

            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowers);
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string targetUserIdStr, CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId))
                return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var cacheKey = $"following:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowing = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                return await unitOfWork.Users.GetFollowingAsync(targetUserId, currentUserId, request);
            });

            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowing);
        }
    }
}
