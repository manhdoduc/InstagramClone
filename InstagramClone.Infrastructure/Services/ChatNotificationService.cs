using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Hubs;
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
}
