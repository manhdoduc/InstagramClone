using InstagramClone.Domain.Common;
using InstagramClone.Domain.Enums;
using System;

namespace InstagramClone.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid RecipientId { get; private set; }
        public AppUser Recipient { get; private set; } = null!;

        public Guid ActorId { get; private set; }
        public AppUser Actor { get; private set; } = null!;

        public NotificationType Type { get; private set; }
        public Guid? TargetId { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }

        private Notification() { }

        public Notification(Guid recipientId, Guid actorId, NotificationType type, string message, Guid? targetId = null)
        {
            Id = Guid.NewGuid();
            RecipientId = recipientId;
            ActorId = actorId;
            Type = type;
            Message = message;
            TargetId = targetId;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
