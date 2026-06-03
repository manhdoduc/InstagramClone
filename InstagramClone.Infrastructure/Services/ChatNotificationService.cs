using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Services;

public class ChatNotificationService(IHubContext<ChatHub, IChatHub> hubContext) : IChatNotificationService
{
    public async Task NotifyNewChatRoomAsync(IEnumerable<string> memberIds, Guid roomId, string message)
    {
        await hubContext.Clients.Users(memberIds).NewChatRoomCreated(roomId, message);
    }

    public async Task NotifyNewChatRoomPrivateAsync(string targetUserId, Guid roomId, string message)
    {
        await hubContext.Clients.User(targetUserId).NewChatRoomCreated(roomId, message);
    }

    // Thông báo khi có tin nhắn mới (Dùng cho cả Text và Media)
    public async Task NotifyReceiveMessageAsync(Guid chatRoomId, MessageDto message)
    {
        await hubContext.Clients.Group(chatRoomId.ToString()).ReceiveMessage(message);
    }

    // Thông báo khi tin nhắn bị thu hồi
    public async Task NotifyMessageUnsentAsync(Guid chatRoomId, Guid messageId)
    {
        await hubContext.Clients.Group(chatRoomId.ToString()).MessageUnsent(messageId);
    }

    // Thông báo khi có người thả tim
    public async Task NotifyMessageReactedAsync(Guid chatRoomId, Guid messageId, string userId, string emoji)
    {
        await hubContext.Clients.Group(chatRoomId.ToString()).MessageReacted(messageId, userId, emoji);
    }
}
