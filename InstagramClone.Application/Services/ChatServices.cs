using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Application.Services
{
    public class ChatServices(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IChatNotificationService chatNotificationService) : IChatService
    {
        public async Task<Result<Guid>> CreateGroupRoomAsync(CreateGroupDto groupDto)
        {
            var currentUserId = currentUser.UserId;
            var nameCurrentUser = context.Users.Where(u => u.Id == currentUserId).Select(u => u.UserName).FirstOrDefault();
            
            var newChatRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                IsGroupChat = true,
                Name = groupDto.GroupName
            };
            context.ChatRooms.Add(newChatRoom);

            var participants = groupDto.MemberIds.Select(memberId => new ChatParticipant
            {
                ChatRoomId = newChatRoom.Id,
                UserId = memberId
            }).ToList();
            //
            participants.Add(new ChatParticipant
            {
                ChatRoomId = newChatRoom.Id,
                UserId = currentUserId
            });

            context.ChatParticipants.AddRange(participants);
            await context.SaveChangesAsync();

            // Thông báo cho tất cả thành viên trong nhóm về phòng chat mới (nếu cần)
            await chatNotificationService.NotifyNewChatRoomAsync(groupDto.MemberIds, newChatRoom.Id, $"{nameCurrentUser} đã thêm bạn vào nhóm {newChatRoom.Name}"!);

            return Result<Guid>.Success(newChatRoom.Id);
        }

        public async Task<Result<Guid>> GetOrCreatePrivateRoomAsync(string targetUserId)
        {
            var currentUserId = currentUser.UserId;
            var nameCurrentUser = context.Users.Where(u => u.Id == currentUserId).Select(u => u.UserName).FirstOrDefault();

            if (currentUserId == targetUserId)
                return Result<Guid>.Failure();

            // Kiểm tra xem đã có phòng chat riêng giữa 2 người chưa
            var existingChatRoom = await context.ChatRooms
                .Where(cr => cr.IsGroupChat == false)
                .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == currentUserId))
                .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == targetUserId))
                .FirstOrDefaultAsync();

            if (existingChatRoom != null)
            {
                return Result<Guid>.Success(existingChatRoom.Id);
            }
            // Nếu chưa có, tạo mới
            var newChatRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                IsGroupChat = false,
                Name = ""
            };
            context.ChatRooms.Add(newChatRoom);

            context.ChatParticipants.AddRange(new[]
            {
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = currentUserId },
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = targetUserId }
            });

            await context.SaveChangesAsync();

            // Thông báo cho người dùng liên quan về phòng chat mới (nếu cần)
            // Chúng ta dùng Clients.User(targetUserId) để bắn tin cho đúng người đó
            // SignalR sẽ tự tìm tất cả các ConnectionId của User B để gửi
            await chatNotificationService.NotifyNewChatRoomPrivateAsync(targetUserId, newChatRoom.Id, nameCurrentUser!);

            return Result<Guid>.Success(newChatRoom.Id);
        }

        public async Task<Result<List<MessageDto>>> GetRoomMessagesAsync(Guid chatRoomId, CursorPaginationRequestMessage messagePagi)
        {
            try
            {
                var currentUserId = currentUser.UserId;
                var isMember = await context.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId);

                if (!isMember)
                    return Result<List<MessageDto>>.Failure();

                var query = context.Messages.AsNoTracking();

                if (messagePagi.Cursor.HasValue)
                {
                    query = query.Where(m => m.CreatedAt < messagePagi.Cursor.Value);
                }

                var messages = await query
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .Include(m => m.Sender) // Include để lấy thông tin người gửi
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(messagePagi.ChatSize + 1)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.UserName!,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        IsRead = m.IsRead
                    })
                    .ToListAsync();

               
                return Result<List<MessageDto>>.Success(messages);
            }
            catch (Exception)
            {
                // Log lỗi nếu cần
                return Result<List<MessageDto>>.Failure(new Error("Error", "Đã xảy ra lỗi khi lấy tin nhắn."));
            }
        }

        // Lấy danh sách phòng chat của người dùng với phân trang
        public async Task<Result<List<ChatRoomDto>>> GetUserChatRoomsAsync()
        {
            var currentUserId = currentUser.UserId;

            var rooms = await context.ChatRooms
                .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == currentUserId))
                .Select(cr => new ChatRoomDto
                {
                    Id = cr.Id,
                    RoomName = cr.IsGroupChat ? cr.Name : cr.ChatParticipant
                        .Where(cp => cp.UserId != currentUserId)
                        .Select(cp => cp.User.UserName)
                        .FirstOrDefault() ?? "Unknown",
                    IsGroupChat = cr.IsGroupChat,
                    LastestMessage = cr.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                    LastestMessageAt = cr.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                    UnreadMessagesCount = cr.Messages.Count(m => m.CreatedAt > DateTime.UtcNow.AddDays(-7) && !m.IsRead && m.SenderId != currentUserId)    
                })
                .OrderByDescending(cr => cr.LastestMessageAt)
                .ToListAsync();

            return Result<List<ChatRoomDto>>.Success(rooms);
        }

        public async Task<Result<bool>> MarkRoomAsReadAsync(Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;
            var unreadMessages = await context.Messages
                .Where(m => m.ChatRoomId == chatRoomId && m.SenderId != currentUserId && !m.IsRead)
                .ToListAsync();

            if(!unreadMessages.Any())
                return Result<bool>.Success(false);

            foreach (var mess in unreadMessages)
            {
                mess.IsRead = true;
            }
            await context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<Message>> SaveMessageAsync(Guid chatRoomId, string content)
        {
            var senderUserId = currentUser.UserId;

            var isMember = await context.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == senderUserId);

            if (!isMember)
                throw new Exception("Bạn không phải là thành viên của phòng chat này.");

            var message = new Message
            {
                ChatRoomId = chatRoomId,
                SenderId = senderUserId,
                Content = content
            };

            //Console.WriteLine($"User {senderUserId} đang gửi tin vào group: {chatRoomId}");

            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var responseMessage =  new Message
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderId = message.SenderId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };
                
            return Result<Message>.Success(responseMessage);
        }
    }
}
