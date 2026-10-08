using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstagramClone.Application.Features.Chat.Mappings;

public static class ChatMappingExtensions
{
    public static MessageDto ToMessageDto(this Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId.ToString(),
            SenderName = message.Sender?.UserName ?? string.Empty,
            Content = message.IsDeleted ? "Message unsent" : (message.Content ?? string.Empty),
            CreatedAt = message.CreatedAt,
            Type = message.Type,
            MediaUrl = message.IsDeleted ? null : message.MediaUrl,
            Reactions = message.Reactions?.Where(r => !r.IsDeleted).Select(r => new ReactionDto
            {
                UserId = r.UserId.ToString(),
                UserName = r.User?.UserName ?? string.Empty,
                Emoji = r.Emoji
            }).ToList() ?? new List<ReactionDto>()
        };
    }

    public static List<MessageDto> ToMessageDtos(this IEnumerable<Message> messages)
    {
        return messages.Select(m => m.ToMessageDto()).ToList();
    }

    public static ChatRoomDto ToChatRoomDto(this ChatRoom room, Guid currentUserId)
    {
        var otherUser = room.ChatParticipant?.FirstOrDefault(cp => cp.UserId != currentUserId)?.User;
        var roomName = room.IsGroupChat
            ? room.Name
            : (otherUser?.UserName ?? "Unknown");

        var lastMsg = room.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        var myParticipant = room.ChatParticipant?.FirstOrDefault(cp => cp.UserId == currentUserId);
        var lastReadAt = myParticipant?.LastReadAt ?? DateTime.MinValue;

        var unreadCount = room.Messages?.Count(m => m.SenderId != currentUserId && m.CreatedAt > lastReadAt) ?? 0;

        return new ChatRoomDto
        {
            Id = room.Id,
            RoomName = roomName,
            IsGroupChat = room.IsGroupChat,
            LastestMessage = lastMsg?.Content ?? string.Empty,
            LastestMessageAt = lastMsg?.CreatedAt ?? room.CreatedAt,
            UnreadMessagesCount = unreadCount
        };
    }

    public static List<ChatRoomDto> ToChatRoomDtos(this IEnumerable<ChatRoom> rooms, Guid currentUserId)
    {
        return rooms.Select(r => r.ToChatRoomDto(currentUserId)).ToList();
    }
}
