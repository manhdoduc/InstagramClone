using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using InstagramClone.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Application.Interfaces.Caching;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Helpers;
using InstagramClone.Application.DTOs.post;

namespace InstagramClone.Application.Services
{
    public class FollowServices(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache, IMapper mapper) : IFollowService
    {
        public async Task<Result<string>> SendFollowRequestAsync(string targetId)
        {
            var observerId = currentUser.UserId;
            // Invalidate follow lists cached per (target, viewer)
            await cache.RemoveAsync($"followers:{targetId}:{observerId}");
            await cache.RemoveAsync($"following:{observerId}:{targetId}");

            if (observerId == targetId)
                return Result<string>.BadRequest(new Error(ErrorCodes.Conflict, "You cannot follow yourself."));

            var targetUser = await unitOfWork.Users.QueryNoTracking().FirstOrDefaultAsync(u => u.Id == targetId);
            if (targetUser == null)
                return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "The user you are trying to follow does not exist."));

            var existingFollow = await unitOfWork.Follows.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == targetId);

            string message = "";

            var initialStatus = targetUser.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;

            if (existingFollow == null)
            {
                unitOfWork.Follows.Add(new Follow
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
                    unitOfWork.Follows.Update(existingFollow);
                    message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                }
                else
                {
                    existingFollow.IsDeleted = true;
                    unitOfWork.Follows.Update(existingFollow);
                    message = FollowCodes.CancelledFollowRequest;
                }
            }
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                // Profile cache: isFollowing/isRequested + follower/following counts depend on follow relation
                await cache.BumpScopeVersionAsync($"user:profile:rev:{targetId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{observerId}");
            }
            return Result<string>.Success(message);
        }

        public async Task<Result<bool>> AcceptFollowRequestAsync(string observerId)
        {
            var userId = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userId}:{observerId}");
            await cache.RemoveAsync($"following:{observerId}:{observerId}");

            if (string.IsNullOrEmpty(userId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to accept follow requests."));

            var request = await unitOfWork.Follows.Query().FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.Status = FollowStatus.Accepted;
            unitOfWork.Follows.Update(request);

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{observerId}");
            }
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to accept follow request."));
        }

        public async Task<Result<bool>> DeclineFollowRequestAsync(string observerId)
        {
            var userId = currentUser.UserId;
            await cache.RemoveAsync($"followers:{userId}:{observerId}");
            await cache.RemoveAsync($"following:{observerId}:{observerId}");

            if (string.IsNullOrEmpty(userId))
                return Result<bool>.BadRequest(new Error(ErrorCodes.Failure, "User must be authenticated to decline follow requests."));

            var request = await unitOfWork.Follows.Query().FirstOrDefaultAsync(f => f.ObserverId == observerId && f.TargetId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.IsDeleted = true;
            unitOfWork.Follows.Update(request);

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{observerId}");
            }
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to decline follow request."));
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowerAsync(string targetUserId, CursorPaginationRequest request)
        {
            var currentUserId = currentUser.UserId;
            var cacheKey = $"followers:{targetUserId}:{currentUserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowers = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var query = unitOfWork.Follows.QueryNoTracking()
                    .Where(f => f.TargetId == targetUserId && f.Status == FollowStatus.Accepted);

                if (request.Cursor.HasValue)
                {
                    query = query.Where(f => f.CreatedAt < request.Cursor.Value);
                }

                var follows = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(request.PageSize + 1)
                    .Select(f => new 
                    {
                        CreatedAt = f.CreatedAt,
                        User = f.Observer,
                        IsFollowing = f.Observer.Followers.Any(follower => follower.ObserverId == currentUserId && follower.Status == FollowStatus.Accepted)
                    })
                    .ToListAsync();

                var hasNextPage = follows.Count > request.PageSize;
                DateTime? nextCursor = null;

                if (hasNextPage)
                {
                    follows.RemoveAt(request.PageSize);
                    nextCursor = follows.Last().CreatedAt;
                }

                var users = follows.Select(f => new UserSummaryDto
                {
                    UserId = f.User.Id,
                    FullName = f.User.FullName,
                    AvatarUrl = f.User.AvatarUrl ?? "",
                    IsFollowing = f.IsFollowing
                }).ToList();

                return new CursorPagedResponse<UserSummaryDto>
                {
                    Items = users,
                    HasNextPage = hasNextPage,
                    NextCursor = nextCursor
                };
            });
           
            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowers);
        }

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string targetUserId, CursorPaginationRequest request)
        {
            var currentUserId = currentUser.UserId;
            var cacheKey = $"following:{targetUserId}:{currentUserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowing = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var query = unitOfWork.Follows.QueryNoTracking()
                    .Where(f => f.ObserverId == targetUserId && f.Status == FollowStatus.Accepted);

                if (request.Cursor.HasValue)
                {
                    query = query.Where(f => f.CreatedAt < request.Cursor.Value);
                }

                var follows = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(request.PageSize + 1)
                    .Select(f => new 
                    {
                        CreatedAt = f.CreatedAt,
                        User = f.Target,
                        IsFollowing = f.Target.Followers.Any(follower => follower.ObserverId == currentUserId && follower.Status == FollowStatus.Accepted)
                    })
                    .ToListAsync();

                var hasNextPage = follows.Count > request.PageSize;
                DateTime? nextCursor = null;

                if (hasNextPage)
                {
                    follows.RemoveAt(request.PageSize);
                    nextCursor = follows.Last().CreatedAt;
                }

                var users = follows.Select(f => new UserSummaryDto
                {
                    UserId = f.User.Id,
                    FullName = f.User.FullName,
                    AvatarUrl = f.User.AvatarUrl ?? "",
                    IsFollowing = f.IsFollowing
                }).ToList();

                return new CursorPagedResponse<UserSummaryDto>
                {
                    Items = users,
                    HasNextPage = hasNextPage,
                    NextCursor = nextCursor
                };
            });
            return Result<CursorPagedResponse<UserSummaryDto>>.Success(cachedFollowing);
        }
    }
}
