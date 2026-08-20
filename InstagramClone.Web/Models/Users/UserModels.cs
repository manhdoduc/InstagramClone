namespace InstagramClone.Web.Models.Users;

/// <summary>Mirror of UserProfileResponseDto from backend.</summary>
public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsPrivateAccount { get; set; }
    public bool MyAccount { get; set; }

    // Stats
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }

    // Relationship
    public bool IsFollowing { get; set; }
    public bool IsRequested { get; set; }

    // Grid preview (12 posts max từ UserServices)
    public List<PostGridItemDto> RecentPosts { get; set; } = new();
}

/// <summary>Mirror of PostGridItemDto từ backend — dùng cho thumbnail grid trên profile.</summary>
public class PostGridItemDto
{
    public Guid Id { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}

/// <summary>Mirror of UserSummaryDto from backend — dùng trong search, follower/following list.</summary>
public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsFollowing { get; set; }
}

/// <summary>DTO để update Bio từ settings.</summary>
public class UpdateBioRequest
{
    public string Bio { get; set; } = string.Empty;
}
