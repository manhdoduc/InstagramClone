using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using InstagramClone.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Application.Interfaces.Caching;

namespace InstagramClone.Application.Services
{
    public class FollowServices(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache) : IFollowService
    {
        public async Task<Result<string>> SendFollowRequestAsync(string targetId)
        {
            var observerId = currentUser.UserId;
            await cache.RemoveAsync($"followers:{targetId}:{observerId}");

            if (observerId == targetId)
                return Result<string>.BadRequest(new Error(ErrorCodes.Conflict, "You cannot follow yourself."));

            var targetUser = await context.Users.FirstOrDefaultAsync(u => u.Id == targetId);
            if (targetUser == null)
                return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "The user you are trying to follow does not exist."));

            var existingFollow = await context.Follows
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == targetId);

            string message = "";

            var initialStatus = targetUser.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;

            if (existingFollow == null)
            {
                context.Follows.Add(new Follow
                {
                    ObserverId = observerId,
                    TargetId = targetId,
                    Status = initialStatus
                });
                message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
            }
            else
            {
                // kịch bản toggle follow/unfollow
                if (existingFollow.IsDeleted)
                {
                    existingFollow.IsDeleted = false;
                    existingFollow.Status = initialStatus;
                    context.Follows.Update(existingFollow);
                    message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                }
                else
                {
                    existingFollow.IsDeleted = true;
                    context.Follows.Update(existingFollow);
                    message = FollowCodes.CancelledFollowRequest;
                }
            }
            await context.SaveChangesAsync();
            return Result<string>.Success(message);
        }

        public async Task<Result<bool>> AcceptFollowRequestAsync(string observerId)
        {
            var userId = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userId}:{observerId}");

            if (string.IsNullOrEmpty(userId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to accept follow requests."));

            var request = await context.Follows.FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.Status = FollowStatus.Accepted;
            context.Follows.Update(request);

            var saved = await context.SaveChangesAsync() > 0;
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to accept follow request."));
        }

        public async Task<Result<bool>> DeclineFollowRequestAsync(string observerId)
        {
            var userId = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userId}:{observerId}");

            if (string.IsNullOrEmpty(userId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to decline follow requests."));

            var request = await context.Follows.FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.IsDeleted = true;
            context.Follows.Update(request);

            var saved = await context.SaveChangesAsync() > 0;
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to decline follow request."));
        }

        public async Task<Result<List<UserSummaryDto>>> GetFollowerAsync(string targetUserId)
        {
            var currentUserId = currentUser.UserId;
            var cacheKey = $"followers:{targetUserId}:{currentUserId}";
            var cachedFollowers = await cache.GetOrCreateAsync<List<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var follower = await context.Follows
                .AsNoTracking()
                .Where(f => f.TargetId == targetUserId && f.Status == FollowStatus.Accepted)
                .Select(f => new UserSummaryDto
                {
                    UserId = f.Observer.Id,
                    UserName = f.Observer.UserName!,
                    FullName = f.Observer.FullName,
                    AvatarUrl = f.Observer.AvatarUrl!,
                    IsFollowing = !string.IsNullOrEmpty(currentUserId)
                        && context.Follows.Any(myFollow => myFollow.ObserverId == currentUserId &&
                                                myFollow.TargetId == f.ObserverId &&
                                                myFollow.Status == FollowStatus.Accepted)
                })
                .ToListAsync();
                return follower;
            });
           
            return Result<List<UserSummaryDto>>.Success(cachedFollowers);
        }

        public async Task<Result<List<UserSummaryDto>>> GetFollowingAsync(string targetUserId)
        {
            var currentUserId = currentUser.UserId;
            var cacheKey = $"following:{targetUserId}:{currentUserId}";
            var cachedFollowing = await cache.GetOrCreateAsync<List<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var following = await context.Follows
                .AsNoTracking()
                .Where(f => f.ObserverId == targetUserId && f.Status == FollowStatus.Accepted)
                .Select(f => new UserSummaryDto
                {
                    UserId = f.Target.Id,
                    UserName = f.Target.UserName!,
                    FullName = f.Target.FullName,
                    AvatarUrl = f.Target.AvatarUrl!,
                    IsFollowing = !string.IsNullOrEmpty(currentUserId)
                        && context.Follows.Any(myFollow => myFollow.ObserverId == currentUserId &&
                                                myFollow.TargetId == f.Target.Id &&
                                                myFollow.Status == FollowStatus.Accepted)
                })
                .ToListAsync();
                return following;
            });
            return Result<List<UserSummaryDto>>.Success(cachedFollowing);
        }
    }
}
