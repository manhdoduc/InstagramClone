using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Common.Helper;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InstagramClone.Infrastructure.Repositories;

public class UserRepository(AppDbContext context, IMapper mapper) : IUserRepository
{
    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        return await context.AppUsers.FindAsync(id);
    }

    public void Add(AppUser user)
    {
        context.AppUsers.Add(user);
    }

    public void Update(AppUser user)
    {
        context.AppUsers.Update(user);
    }

    public async Task<bool> AnyAsync(Expression<Func<AppUser, bool>> predicate)
    {
        return await context.AppUsers.AnyAsync(predicate);
    }

    public async Task<UserProfileResponseDto?> GetUserProfileAsync(Guid targetUserId, Guid currentUserId)
    {
        return await context.AppUsers.AsNoTracking()
            .Where(u => u.Id == targetUserId)
            .ProjectTo<UserProfileResponseDto>(mapper.ConfigurationProvider, new { currentUserId, targetUserId })
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserSummaryDto>> SearchUsersAsync(string searchTerm, Guid currentUserId)
    {
        searchTerm = RemoveDiacritics.RemoveDiacritic(searchTerm.Trim());
        var query = context.AppUsers.AsNoTracking();

        if (searchTerm.StartsWith("@"))
        {
            string usernameSearch = searchTerm.Substring(1);
            query = query.Where(u => u.UserName == usernameSearch);
        }
        else
        {
            query = query.Where(u => u.FullNameSearch != null && u.FullNameSearch.Contains(searchTerm));
        }

        return await query
            .Take(15)
            .ProjectTo<UserSummaryDto>(mapper.ConfigurationProvider, new { currentUserId })
            .ToListAsync();
    }

    public async Task<Follow?> GetFollowAsync(Guid followerId, Guid followeeId, bool includeDeleted = false)
    {
        var query = context.Follows.AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
    }

    public async Task<Follow?> GetPendingFollowRequestAsync(Guid followerId, Guid followeeId)
    {
        return await context.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId && f.Status == FollowStatus.Pending);
    }

    public void AddFollow(Follow follow)
    {
        context.Follows.Add(follow);
    }

    public void UpdateFollow(Follow follow)
    {
        context.Follows.Update(follow);
    }

    public async Task<List<Guid>> GetFollowingIdsAsync(Guid userId)
    {
        return await context.Follows.AsNoTracking()
            .Where(f => f.FollowerId == userId && f.Status == FollowStatus.Accepted)
            .Select(f => f.FolloweeId)
            .ToListAsync();
    }

    public async Task<CursorPagedResponse<UserSummaryDto>> GetFollowersAsync(Guid targetUserId, Guid currentUserId, CursorPaginationRequest request)
    {
        var query = context.Follows.AsNoTracking()
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
                f.CreatedAt,
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
    }

    public async Task<CursorPagedResponse<UserSummaryDto>> GetFollowingAsync(Guid targetUserId, Guid currentUserId, CursorPaginationRequest request)
    {
        var query = context.Follows.AsNoTracking()
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
                f.CreatedAt,
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
    }
}
