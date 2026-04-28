using InstagramClone.Application.DTOs.ChatDto;
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

        // 1. QUẢN LÝ ONLINE / OFFLINE

        public override async Task OnConnectedAsync()
        {
            var userId = currentUser.UserId;

            _onlineUsers.AddOrUpdate(userId, new HashSet<string> { Context.ConnectionId }, (key, existingSet) =>
            {
                lock (existingSet)
                    existingSet.Add(Context.ConnectionId);
                
                return existingSet;
            });
            await Clients.Others.UserOnline(userId); 
            // có thể dùng redis pub/sub để broadcast sự kiện online/offline này đến tất cả instance của app, thay vì chỉ broadcast trong instance hiện tại như thế này

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
            await Clients.Others.UserOffline(userId);
            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<string>> GetOnlineUsers()
        {
            var onlineUserIds = _onlineUsers.Keys.ToList();
            return Task.FromResult(onlineUserIds);
        }

        // 2. LUỒNG CHAT 

        public async Task SendMessage(SendMessageDto sendMessage)
        {
            var result = await chatService.CreateMessageAsync(sendMessage); // lưu message vào database, không cần await vì chúng ta sẽ gửi message đi trước, nếu có lỗi khi lưu thì có thể xử lý sau
            if (result.IsSuccess)
                await Clients.Group(sendMessage.ChatRoomId.ToString()).ReceiveMessage(result.Value!);
        }

        public async Task JoinChatRoom(string targetUserId, Guid chatRoomId)
        {
            var result = await chatService.AddMemberToGroupAsync(targetUserId, chatRoomId);
            if (result.IsSuccess)
                await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString());
        }

        public async Task LeaveChatRoom(Guid chatRoomId)
        {
            var result = await chatService.LeaveGroupAsync(chatRoomId);
            if(result.IsSuccess)
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

        public async Task StartTyping(Guid chatRoomId)
        {
            var userId = currentUser.UserId;
            // Bắn tin cho những người KHÁC trong phòng
            await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
                         .UserTyping(userId, chatRoomId);
        }

        public async Task StopTyping(Guid chatRoomId)
        {
            var userId = currentUser.UserId;
            await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
                         .UserStoppedTyping( userId, chatRoomId);
        }
        // Chúng ta có thể thêm các phương thức khác như StartTyping, StopTyping để thông báo cho người dùng khác biết khi nào một người đang gõ tin nhắn, nhưng trong bản demo này chúng ta sẽ không triển khai tính năng đó để giữ cho code đơn giản hơn
        // Làm khi có giao diện


        // Trong ChatHub.cs

        public async Task UnsendMessage(Guid messageId, Guid chatRoomId)
        {
            var result = await chatService.UnsendMessageAsync(messageId);
            if (result.IsSuccess)
            {
                // Thông báo cho cả phòng ẩn tin nhắn này đi
                await Clients.Group(chatRoomId.ToString()).MessageUnsent(messageId);
            }
        }

        public async Task ReactToMessage(Guid messageId, Guid chatRoomId, string emoji)
        {
            var result = await chatService.ReactToMessageAsync(messageId, emoji);
            if (result.IsSuccess)
            {
                // Thông báo cho cả phòng cập nhật lại danh sách tim
                var userId = currentUser.UserId;
                await Clients.Group(chatRoomId.ToString()).MessageReacted(messageId, userId, emoji);
            }
        }
    }
}