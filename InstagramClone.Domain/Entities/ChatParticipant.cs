using InstagramClone.Domain.Common;

namespace InstagramClone.Domain.Entities
{
    public class ChatParticipant : BaseEntity
    {
        public Guid ChatRoomId { get; private set; }
        public virtual ChatRoom ChatRoom { get; private set; } = null!;

        public Guid UserId { get; private set; }
        public virtual AppUser User { get; private set; } = null!;

        public bool IsAdmin { get; private set; } = false;

        public DateTime LastReadAt { get; private set; } = DateTime.UtcNow;

        public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

        protected ChatParticipant() { }

        public ChatParticipant(Guid chatRoomId, Guid userId, bool isAdmin = false)
        {
            ChatRoomId = chatRoomId;
            UserId = userId;
            IsAdmin = isAdmin;
            LastReadAt = DateTime.UtcNow;
            JoinedAt = DateTime.UtcNow;
        }

        public void UpdateLastRead()
        {
            LastReadAt = DateTime.UtcNow;
        }
    }
}