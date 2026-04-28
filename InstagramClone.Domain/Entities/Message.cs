using InstagramClone.Domain.Common;

namespace InstagramClone.Domain.Entities
{
    public enum MessageType { Text, Image, Video, Voice, File }

    public class Message : BaseEntity
    {
        public Guid ChatRoomId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? Content { get; set; } // Dùng cho Text hoặc chú thích ảnh

        // TRƯỜNG MỚI
        public MessageType Type { get; set; } = MessageType.Text;
        public string? MediaUrl { get; set; }

        // Tính năng Reply (Self-referencing)
        public Guid? ReplyToMessageId { get; set; }
        public virtual Message? ReplyToMessage { get; set; }

        // Navigation properties
        public virtual AppUser Sender { get; set; } = null!;
        public virtual ChatRoom ChatRoom { get; set; } = null!;

        // Thêm dòng này vào class Message
        public virtual ICollection<MessageReaction> Reactions { get; set; } = [];
    }
}