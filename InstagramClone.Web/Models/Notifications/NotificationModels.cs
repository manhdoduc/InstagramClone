namespace InstagramClone.Web.Models.Notifications;

/// <summary>Mirror of NotificationDto from backend.</summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string ActorUsername { get; set; } = string.Empty;
    public string? ActorAvatarUrl { get; set; }
    public NotificationType Type { get; set; }
    public Guid? TargetId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Mirror of NotificationType enum từ backend Domain.</summary>
public enum NotificationType
{
    LikePost = 1,
    Comment = 2,
    Follow = 3,
    FollowRequest = 4,
    FollowAccept = 5
}
