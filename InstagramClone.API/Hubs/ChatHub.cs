using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace InstagramClone.API.Hubs
{
    [Authorize]
    public class ChatHub(AppDbContext context, ICurrentUserService currentUser) : Hub<IChatHub>
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();
        // sau dùng redis pub/sub để quản lý online/offline thay vì dùng static dictionary này, vì static dictionary này sẽ bị reset khi app restart, và không thể scale ra nhiều instance được

        // ==========================================
        // 1. QUẢN LÝ ONLINE / OFFLINE
        // ==========================================
        public override async Task OnConnectedAsync()
        {
            var userId = currentUser.UserId;

            _onlineUsers.AddOrUpdate(userId, new HashSet<string> { Context.ConnectionId }, (key, existingSet) =>
            {
                lock (existingSet)
                    existingSet.Add(Context.ConnectionId);
                
                return existingSet;
            });
            // await Clients.Others.UserOnline(userId); // có thể dùng redis pub/sub để broadcast sự kiện online/offline này đến tất cả instance của app, thay vì chỉ broadcast trong instance hiện tại như thế này

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = currentUser.UserId;
            if (_onlineUsers.TryGetValue(userId, out var connections))
            {
                lock(connections)
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        _onlineUsers.TryRemove(userId, out _);
                    }
                }
            }
            // await Clients.Others.UserOffline(userId);
            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<string>> GetOnlineUsers()
        {
            var onlineUserIds = _onlineUsers.Keys.ToList();
            return Task.FromResult(onlineUserIds);
        }

        // ==========================================
        // 2. LUỒNG CHAT 
        // ==========================================

        public async Task SendMessage(Guid chatRoomId, string content)
        {
            var senderUserId = currentUser.UserId;

            var isMember = await context.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == senderUserId);

            if(!isMember)
                throw new HubException("Bạn không phải là thành viên của phòng chat này.");

            var message = new Message
            {
                ChatRoomId = chatRoomId,
                SenderId = senderUserId,
                Content = content
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();

            await Clients.Group(chatRoomId.ToString()).ReceiveMessage(new Message
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            });
        }

        public async Task JoinChatRoom(Guid chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        public async Task LeaveChatRoom(Guid chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }
    }
}