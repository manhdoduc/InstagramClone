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
            // có th? dùng redis pub/sub d? broadcast s? ki?n online/offline này d?n t?t c? instance c?a app, thay vì ch? broadcast trong instance hi?n t?i nhu th? này

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

        // 2. LU?NG CHAT 

        public async Task SendMessage(SendMessageDto sendMessage)
        {
            var result = await chatService.CreateMessageAsync(sendMessage); // luu message vào database, không c?n await vì chúng ta s? g?i message di tru?c, n?u có l?i khi luu thì có th? x? lý sau
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

        // dánh d?u t?t c? tin nh?n trong phòng chat là dã d?c
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
            // B?n tin cho nh?ng ngu?i KHÁC trong phòng
            await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
                         .UserTyping(userId, chatRoomId);
        }

        public async Task StopTyping(Guid chatRoomId)
        {
            var userId = currentUser.UserId;
            await Clients.OthersInGroup(chatRoomId.ToString().ToLower())
                         .UserStoppedTyping( userId, chatRoomId);
        }
        // Chúng ta có th? thêm các phuong th?c khác nhu StartTyping, StopTyping d? thông báo cho ngu?i dùng khác bi?t khi nào m?t ngu?i dang gõ tin nh?n, nhung trong b?n demo này chúng ta s? không tri?n khai tính nang dó d? gi? cho code don gi?n hon
        // Làm khi có giao di?n


        // Trong ChatHub.cs

        public async Task UnsendMessage(Guid messageId, Guid chatRoomId)
        {
            var result = await chatService.UnsendMessageAsync(messageId);
            if (result.IsSuccess)
            {
                // Thông báo cho c? phòng ?n tin nh?n này di
                await Clients.Group(chatRoomId.ToString()).MessageUnsent(messageId);
            }
        }

        public async Task ReactToMessage(Guid messageId, Guid chatRoomId, string emoji)
        {
            var result = await chatService.ReactToMessageAsync(messageId, emoji);
            if (result.IsSuccess)
            {
                // Thông báo cho c? phòng c?p nh?t l?i danh sách tim
                var userId = currentUser.UserId;
                await Clients.Group(chatRoomId.ToString()).MessageReacted(messageId, userId, emoji);
            }
        }
    }
}