using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstagramClone.Application.Features.Users.Mappings;

public static class UserMappingExtensions
{
    public static UserProfileResponseDto ToUserProfileResponseDto(this AppUser user, Guid currentUserId)
    {
        return new UserProfileResponseDto
        {
            Id = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            IsPrivateAccount = user.IsPrivateAccount,
            MyAccount = user.Id == currentUserId,
            PostCount = user.Posts?.Count(p => !p.IsDeleted) ?? 0,
            FollowerCount = user.Followers?.Count(f => f.Status == FollowStatus.Accepted && !f.IsDeleted) ?? 0,
            FollowingCount = user.Followings?.Count(f => f.Status == FollowStatus.Accepted && !f.IsDeleted) ?? 0,
            IsFollowing = user.Followers?.Any(f => f.FollowerId == currentUserId && f.Status == FollowStatus.Accepted && !f.IsDeleted) ?? false,
            IsRequested = user.Followers?.Any(f => f.FollowerId == currentUserId && f.Status == FollowStatus.Pending && !f.IsDeleted) ?? false
        };
    }

    public static UserSummaryDto ToUserSummaryDto(this AppUser user, Guid currentUserId)
    {
        return new UserSummaryDto
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            IsFollowing = user.Followers?.Any(f => f.FollowerId == currentUserId && f.Status == FollowStatus.Accepted && !f.IsDeleted) ?? false
        };
    }

    public static List<UserSummaryDto> ToUserSummaryDtos(this IEnumerable<AppUser> users, Guid currentUserId)
    {
        return users.Select(u => u.ToUserSummaryDto(currentUserId)).ToList();
    }
}
