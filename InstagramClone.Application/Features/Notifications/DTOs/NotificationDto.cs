using InstagramClone.Domain.Enums;
using System;

namespace InstagramClone.Application.Features.Notifications.DTOs
{
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
}
