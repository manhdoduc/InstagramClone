using InstagramClone.Application.DTOs.Chats;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;

public interface IChatNotificationService
{
    Task NotifyNewChatRoomAsync(IEnumerable<string> memberIds, Guid roomId, string message);
    Task NotifyNewChatRoomPrivateAsync(string targetUserId, Guid roomId, string message);
    Task NotifyReceiveMessageAsync(Guid chatRoomId, MessageDto message);
    Task NotifyMessageUnsentAsync(Guid chatRoomId, Guid messageId);
    Task NotifyMessageReactedAsync(Guid chatRoomId, Guid messageId, string userId, string emoji);
}
