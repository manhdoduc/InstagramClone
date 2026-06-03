using InstagramClone.Domain.Common;

namespace InstagramClone.Domain.Entities
{
    public enum MessageType { Text, Image, Video, Voice, File }

    public class Message : BaseEntity
    {
        public Guid ChatRoomId { get; private set; }
        public Guid SenderId { get; private set; }
        public string? Content { get; private set; } // Dùng cho Text hoặc chú thích ảnh

        // TRƯỜNG MỚI
        public MessageType Type { get; private set; } = MessageType.Text;
        public string? MediaUrl { get; private set; }

        // Tính năng Reply (Self-referencing)
        public Guid? ReplyToMessageId { get; private set; }
        public virtual Message? ReplyToMessage { get; private set; }

        // Navigation properties
        public virtual AppUser Sender { get; private set; } = null!;
        public virtual ChatRoom ChatRoom { get; private set; } = null!;

        // Thêm dòng này vào class Message
        private readonly List<MessageReaction> _reactions = [];
        public virtual IReadOnlyCollection<MessageReaction> Reactions => _reactions.AsReadOnly();

        protected Message() { }

        public Message(Guid chatRoomId, Guid senderId, MessageType type, string? content = null, string? mediaUrl = null, Guid? replyToMessageId = null)
        {
            ChatRoomId = chatRoomId;
            SenderId = senderId;
            Type = type;
            Content = content;
            MediaUrl = mediaUrl;
            ReplyToMessageId = replyToMessageId;
        }

        public void Unsend()
        {
            MarkAsDeleted();
            Content = "Message unsent";
            MediaUrl = null;
        }
    }
}
