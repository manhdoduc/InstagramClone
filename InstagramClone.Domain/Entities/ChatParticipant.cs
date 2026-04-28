namespace InstagramClone.Domain.Entities
{
    public class ChatParticipant
    {
        public Guid ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public virtual AppUser User { get; set; } = null!;

        public bool IsAdmin { get; set; } = false;

        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}