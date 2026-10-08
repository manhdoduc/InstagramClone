using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface IChatRepository
{
    // Rooms
    Task<ChatRoom?> GetRoomByIdAsync(Guid id);
    void AddRoom(ChatRoom room);
    Task<ChatRoom?> GetPrivateRoomAsync(Guid userA, Guid userB);

    // Participants
    Task<ChatParticipant?> GetParticipantAsync(Guid chatRoomId, Guid userId);
    Task<bool> IsParticipantAsync(Guid chatRoomId, Guid userId);
    Task<bool> IsRoomAdminAsync(Guid chatRoomId, Guid userId);
    void AddParticipant(ChatParticipant participant);
    void AddParticipants(IEnumerable<ChatParticipant> participants);
    void RemoveParticipant(ChatParticipant participant);
    Task<int> MarkRoomAsReadAsync(Guid chatRoomId, Guid userId);

    // Messages
    Task<Message?> GetMessageByIdAsync(Guid messageId);
    Task<Message?> GetUserMessageByIdAsync(Guid messageId, Guid userId);
    void AddMessage(Message message);
    Task<List<Message>> GetRoomMessagesAsync(Guid chatRoomId, DateTime? cursor, int pageSize);
    Task<List<ChatRoom>> GetUserChatRoomsAsync(Guid userId);

    // Reactions
    Task<MessageReaction?> GetReactionAsync(Guid messageId, Guid userId);
    void AddReaction(MessageReaction reaction);
    void RemoveReaction(MessageReaction reaction);
}
