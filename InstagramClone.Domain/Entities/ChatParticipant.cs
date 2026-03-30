namespace InstagramClone.Domain.Entities
{
    public class ChatParticipant
    {
        public Guid ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public virtual AppUser User { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}