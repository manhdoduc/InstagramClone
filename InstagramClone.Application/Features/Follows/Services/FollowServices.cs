using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Common;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Application.Features.Follows.Services{
    public class FollowServices(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache, IMapper mapper) : IFollowService
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

            var targetUser = await unitOfWork.Users.QueryNoTracking().FirstOrDefaultAsync(u => u.Id == followeeGuid);
            if (targetUser == null)
                return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "The user you are trying to follow does not exist."));

            var existingFollow = await unitOfWork.Follows.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.FollowerId == followerGuid && f.FolloweeId == followeeGuid);

            string message = "";

            var initialStatus = targetUser.IsPrivateAccount ? FollowStatus.Pending : FollowStatus.Accepted;

            if (existingFollow == null)
            {
                unitOfWork.Follows.Add(new Follow(followerGuid, followeeGuid, initialStatus));
                message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
            }
            else
            {
                // kịch bản toggle follow/unfollow
                if (existingFollow.IsDeleted)
                {
                    existingFollow.Restore();
                    existingFollow.UpdateStatus(initialStatus);
                    unitOfWork.Follows.Update(existingFollow);
                    message = targetUser.IsPrivateAccount ? FollowCodes.FollowRequestSent : FollowCodes.Followed;
                }
                else
                {
                    existingFollow.MarkAsDeleted();
                    unitOfWork.Follows.Update(existingFollow);
                    message = FollowCodes.CancelledFollowRequest;
                }
            }
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                // Profile cache: isFollowing/isRequested + follower/following counts depend on follow relation
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followeeIdStr}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followerId}");
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

            var request = await unitOfWork.Follows.Query().FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.UpdateStatus(FollowStatus.Accepted);
            unitOfWork.Follows.Update(request);

            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
            {
                await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
                await cache.BumpScopeVersionAsync($"user:profile:rev:{followerId}");
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

            var request = await unitOfWork.Follows.Query().FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == userId && f.Status == FollowStatus.Pending);
            if (request == null)
                return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "Follow request not found."));

            request.MarkAsDeleted();
            unitOfWork.Follows.Update(request);

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
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId)) return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
            var cacheKey = $"followers:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowers = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var query = unitOfWork.Follows.QueryNoTracking()
                    .Where(f => f.FolloweeId == targetUserId && f.Status == FollowStatus.Accepted);

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
                        User = f.Follower,
                        IsFollowing = f.Follower.Followers.Any(follower => follower.FollowerId == currentUserId && follower.Status == FollowStatus.Accepted)
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
                    UserId = f.User.Id.ToString(),
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

        public async Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string targetUserIdStr, CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var currentUserId) || !Guid.TryParse(targetUserIdStr, out var targetUserId)) return Result<CursorPagedResponse<UserSummaryDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
            var cacheKey = $"following:{targetUserIdStr}:{currentUser.UserId}:{request.PageSize}:{request.Cursor}";
            var cachedFollowing = await cache.GetOrCreateAsync<CursorPagedResponse<UserSummaryDto>>(cacheKey, factory: async () =>
            {
                var query = unitOfWork.Follows.QueryNoTracking()
                    .Where(f => f.FollowerId == targetUserId && f.Status == FollowStatus.Accepted);

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
                        User = f.Followee,
                        IsFollowing = f.Followee.Followers.Any(follower => follower.FollowerId == currentUserId && follower.Status == FollowStatus.Accepted)
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
                    UserId = f.User.Id.ToString(),
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
