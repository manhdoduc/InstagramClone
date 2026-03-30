using InstagramClone.Domain.Common;

namespace InstagramClone.Domain.Entities
{
    public class Message : BaseEntity
    {
        public Guid ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;

        public string Content { get; set; } = null!;
        public bool IsRead { get; set; } = false;
    }
}