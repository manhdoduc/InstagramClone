using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace InstagramClone.Infrastructure.Hubs
{
    [Authorize]
    public class ChatHub(ICurrentUserService currentUser, IChatService chatService) : Hub<IChatHub>
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
            var result = await chatService.SaveMessageAsync(chatRoomId, content); // lưu message vào database, không cần await vì chúng ta sẽ gửi message đi trước, nếu có lỗi khi lưu thì có thể xử lý sau
            if (result.IsSuccess)
                await Clients.Group(chatRoomId.ToString()).ReceiveMessage(result.Value);
        }

        public async Task JoinChatRoom(Guid chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        public async Task LeaveChatRoom(Guid chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        // đánh dấu tất cả tin nhắn trong phòng chat là đã đọc
        public async Task MarkMessagesAsRead(Guid chatRoomId)
        {
            var userId = currentUser.UserId;
            var result = await chatService.MarkRoomAsReadAsync(chatRoomId);
            if (result.IsSuccess && result.Value)
            {
                await Clients.Group(chatRoomId.ToString()).MessagesRead(userId, chatRoomId);
            }
        }

        //public async Task StartTyping(Guid chatRoomId)
        //{
        //    var userId = currentUser.UserId;
        //    // Bắn tin cho những người KHÁC trong phòng
        //    await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
        //                 .SendAsync("UserTyping", userId, chatRoomId);
        //}

        //public async Task StopTyping(Guid chatRoomId)
        //{
        //    var userId = currentUser.UserId;
        //    await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
        //                 .SendAsync("UserStoppedTyping", userId, chatRoomId);
        //}
        // Chúng ta có thể thêm các phương thức khác như StartTyping, StopTyping để thông báo cho người dùng khác biết khi nào một người đang gõ tin nhắn, nhưng trong bản demo này chúng ta sẽ không triển khai tính năng đó để giữ cho code đơn giản hơn
        // Làm khi có giao diện

    }
}