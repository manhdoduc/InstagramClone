using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace InstagramClone.Infrastructure.SignalR{
    [Authorize]
    public class ChatHub(ICurrentUserService currentUser, IChatService chatService) : Hub<IChatHub>
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineUsers = new();
        // sau dùng redis pub/sub d? qu?n lý online/offline thay vì dùng static dictionary này, vì static dictionary này s? b? reset khi app restart, và không th? scale ra nhi?u instance du?c

        // 1. QU?N LÝ ONLINE / OFFLINE

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

            // Tự động join connection này vào tất cả các SignalR Group phòng chat mà user là thành viên
            var userRoomsResult = await chatService.GetUserChatRoomsAsync();
            if (userRoomsResult.IsSuccess && userRoomsResult.Value != null)
            {
                foreach (var room in userRoomsResult.Value)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString().ToLower());
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = currentUser.UserId;
            var isTotallyOffline = false;

            if (_onlineUsers.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        _onlineUsers.TryRemove(userId, out _);
                        isTotallyOffline = true; // Chỉ offline khi xóa sạch connection
                    }
                }
            }

            if (isTotallyOffline)
            {
                await Clients.Others.UserOffline(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<string>> GetOnlineUsers()
        {
            var onlineUserIds = _onlineUsers.Keys.ToList();
            return Task.FromResult(onlineUserIds);
        }

        // 2. LUỒNG CHAT 

        // Cho phép Client chủ động gia nhập một SignalR Group phòng chat (ví dụ khi vừa tạo room mới hoặc vừa mở màn hình room)
        public async Task JoinRoom(Guid chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString().ToLower());
        }

        public async Task SendMessage(SendMessageDto sendMessage)
        {
            await chatService.CreateMessageAsync(sendMessage); 
        }

        public async Task JoinChatRoom(string targetUserId, Guid chatRoomId)
        {
            var result = await chatService.AddMemberToGroupAsync(targetUserId, chatRoomId);
            if (result.IsSuccess)
                await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId.ToString().ToLower());
        }

        public async Task LeaveChatRoom(Guid chatRoomId)
        {
            var result = await chatService.LeaveGroupAsync(chatRoomId);
            if (result.IsSuccess)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId.ToString().ToLower());
        }

        // Đánh dấu tất cả tin nhắn trong phòng chat là đã đọc
        public async Task MarkMessagesAsRead(Guid chatRoomId)
        {
            var userId = currentUser.UserId;
            var result = await chatService.MarkRoomAsReadAsync(chatRoomId);
            if (result.IsSuccess && result.Value)
            {
                await Clients.Group(chatRoomId.ToString().ToLower()).MessagesRead(userId, chatRoomId);
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
                         .UserStoppedTyping(userId, chatRoomId);
        }

        public async Task UnsendMessage(Guid messageId, Guid chatRoomId)
        {
            await chatService.UnsendMessageAsync(messageId);
        }

        public async Task ReactToMessage(Guid messageId, Guid chatRoomId, string emoji)
        {
            await chatService.ReactToMessageAsync(messageId, emoji);
        }
    }
}