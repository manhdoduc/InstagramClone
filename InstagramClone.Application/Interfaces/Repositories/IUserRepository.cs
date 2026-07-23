using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id);
    void Add(AppUser user);
    void Update(AppUser user);
    Task<bool> AnyAsync(Expression<Func<AppUser, bool>> predicate);

    Task<UserProfileResponseDto?> GetUserProfileAsync(Guid targetUserId, Guid currentUserId);
    Task<List<UserSummaryDto>> SearchUsersAsync(string searchTerm, Guid currentUserId);

    // Follows
    Task<Follow?> GetFollowAsync(Guid followerId, Guid followeeId, bool includeDeleted = false);
    Task<Follow?> GetPendingFollowRequestAsync(Guid followerId, Guid followeeId);
    void AddFollow(Follow follow);
    void UpdateFollow(Follow follow);
    Task<List<Guid>> GetFollowingIdsAsync(Guid userId);
    Task<CursorPagedResponse<UserSummaryDto>> GetFollowersAsync(Guid targetUserId, Guid currentUserId, CursorPaginationRequest request);
    Task<CursorPagedResponse<UserSummaryDto>> GetFollowingAsync(Guid targetUserId, Guid currentUserId, CursorPaginationRequest request);
}
