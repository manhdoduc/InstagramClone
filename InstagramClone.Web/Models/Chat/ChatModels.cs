namespace InstagramClone.Web.Models.Chat;

/// <summary>Mirror of ChatRoomDto from backend.</summary>
public class ChatRoomDto
{
    public Guid Id { get; set; }
    public string? RoomName { get; set; }
    public bool IsGroupChat { get; set; }
    public string? LatestMessage { get; set; }
    public DateTime? LatestMessageAt { get; set; }
    public int UnreadMessagesCount { get; set; }
}

/// <summary>Mirror of MessageDto from backend.</summary>
public class MessageDto
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string? MediaUrl { get; set; }
    public List<ReactionDto> Reactions { get; set; } = new();
}

/// <summary>Mirror of SendMessageDto from backend.</summary>
public class SendMessageRequest
{
    public Guid ChatRoomId { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string? MediaUrl { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

/// <summary>Mirror of CreateGroupDto from backend.</summary>
public class CreateGroupRequest
{
    public string GroupName { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
}

/// <summary>Mirror của ReactionDto từ backend.</summary>
public class ReactionDto
{
    public string UserId { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}

/// <summary>Mirror of MessageType enum từ backend Domain.</summary>
public enum MessageType
{
    Text,
    Image,
    Video,
    Voice,
    File
}
