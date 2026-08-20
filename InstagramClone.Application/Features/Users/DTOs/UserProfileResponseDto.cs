using System;
using System.Collections.Generic;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Features.Users.DTOs;

public class UserProfileResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsPrivateAccount { get; set; }
    public bool MyAccount { get; set; }

    public int PostCount { get; set; } = 0;
    public int FollowerCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;

    public bool IsFollowing { get; set; }
    public bool IsRequested { get; set; }

    public List<PostGridItemDto> RecentPosts { get; set; } = new();
}

public class PostGridItemDto
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}
