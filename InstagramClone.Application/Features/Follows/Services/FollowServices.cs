using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Follows.Services
{
    public class FollowServices(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache, IBackgroundJobService backgroundJobService) : IFollowService
    {
        public async Task<Result<string>> SendFollowRequestAsync(string followeeIdStr)
        {
            var followerId = currentUser.UserId;

            if (string.IsNullOrEmpty(followerId) || !Guid.TryParse(followerId, out var followerGuid) || !Guid.TryParse(followeeIdStr, out var followeeGuid))
                return Result<string>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to follow someone."));

            if (followerId == followeeIdStr)
                return Result<string>.BadRequest(new Error(ErrorCodes.Conflict, "You cannot follow yourself."));

            var followee = await unitOfWork.Users.GetByIdAsync(followeeGuid);
            if (followee == null)
                return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

            var existingFollow = await unitOfWork.Users.GetFollowAsync(followerGuid, followeeGuid, includeDeleted: true);

            string message;
            bool shouldNotify = false;
            NotificationType notifyType = NotificationType.Follow;

            if (existingFollow == null)
            {
                var initialStatus = followee.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;
                var newFollow = new Follow(followerGuid, followeeGuid, initialStatus);
                unitOfWork.Users.AddFollow(newFollow);

                message = followee.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                shouldNotify = true;
                notifyType = followee.IsPrivateAccount ? NotificationType.FollowRequest : NotificationType.Follow;
            }
            else
            {
                if (!existingFollow.IsDeleted)
                {
                    existingFollow.MarkAsDeleted();
                    unitOfWork.Users.UpdateFollow(existingFollow);
                    message = FollowCodes.CancelledFollowRequest;
                }
                else
                {
                    var reFollowStatus = followee.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;
                    existingFollow.Restore();
                    existingFollow.UpdateStatus(reFollowStatus);
                    unitOfWork.Users.UpdateFollow(existingFollow);

                    message = followee.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                    shouldNotify = true;
                    notifyType = followee.IsPrivateAccount ? NotificationType.FollowRequest : NotificationType.Follow;
                }
            }
            try
            {
                await unitOfWork.SaveChangesAsync();

                if (shouldNotify)
                {
                    string notifyMsg = notifyType == NotificationType.FollowRequest
                        ? "requested to follow you."
                        : "started following you.";
                    backgroundJobService.Enqueue<INotificationServices>(svc =>
                        svc.CreateAndSendNotificationAsync(followeeGuid, followerGuid, notifyType, notifyMsg, null));
                }

                return Result<string>.Success(message);
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // Race condition: 2 request cùng gửi Follow đồng thời → UNIQUE constraint reject.
                // Idempotent: Follow đã tồn tại → trả về Success vì đây là kết quả đúng mong muốn.
                Log.Warning("Race condition on Follow: FollowerId={FollowerId}, FolloweeId={FolloweeId}. Treating as success.", followerGuid, followeeGuid);
                return Result<string>.Success(message);
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Failed to save follow relationship: FollowerId={FollowerId}, FolloweeId={FolloweeId}", followerGuid, followeeGuid);
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to process follow request."));
            }
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
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to decline follow request."));
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowerAsync(string targetUserIdStr, CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId))
                return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var cacheKey = $"followers:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowers = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(
                cacheKey, 
                factory: async () =>
                {
                    return await unitOfWork.Users.GetFollowersAsync(targetUserId, currentUserId, request);
                },
                TimeSpan.FromSeconds(30));

            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowers);
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string targetUserIdStr, CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId))
                return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var cacheKey = $"following:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowing = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(
                cacheKey, 
                factory: async () =>
                {
                    return await unitOfWork.Users.GetFollowingAsync(targetUserId, currentUserId, request);
                },
                TimeSpan.FromSeconds(30));

            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowing);
        }

        /// <summary>
        /// Kiểm tra DbUpdateException có phải do UNIQUE constraint violation (race condition) không.
        /// SQL Server: 2627 (PK/UNIQUE violation), 2601 (unique index).
        /// </summary>
        private static bool IsDuplicateKeyException(DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? string.Empty;
            return msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("2627", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("2601", StringComparison.OrdinalIgnoreCase);
        }
    }
}
