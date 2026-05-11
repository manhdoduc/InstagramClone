using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Xml.XPath;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Interfaces;

namespace InstagramClone.Application.Services
{
    public class ChatServices(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ICacheService cache,
        IChatNotificationService chatNotificationService,
        IStorageServices storageServices,
        IMapper mapper
        ) : IChatService
    {
        // 
        public async Task<Result<bool>> AddMemberToGroupAsync(string targetUserId, Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;

            var isPrivate = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Id == targetUserId && u.IsPrivateAccount == false);
            if (isPrivate == null) return Result<bool>.Failure(new Error(ErrorCodes.Failure, "User is private"));

            // 1. Kiểm tra xem người thực hiện lệnh có ở trong nhóm không
            var isCurrentMember = await unitOfWork.ChatParticipants.Query()
                .AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId);
            if (!isCurrentMember)
                return Result<bool>.Failure(new Error(ErrorCodes.Failure, "user is already in the group"));

            // 2. Kiểm tra xem targetUserId đã là thành viên chưa
            var isTargetAlreadyMember = await unitOfWork.ChatParticipants.Query()
                .AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == targetUserId);
            if (isTargetAlreadyMember)
                return Result<bool>.Failure(new Error(ErrorCodes.Failure, "user is member"));

            // 3. Thêm thành viên mới
            var newParticipant = new ChatParticipant { ChatRoomId = chatRoomId, UserId = targetUserId };
            unitOfWork.ChatParticipants.Add(newParticipant);
            await unitOfWork.SaveChangesAsync();

            // 4. Dọn Cache Inbox cho người mới để họ thấy nhóm này hiện lên sidebar
            await cache.RemoveAsync($"chat:inbox:{targetUserId}");

            // 5. Gửi thông báo SignalR (Dùng ChatNotificationService của bạn)
            var room = await unitOfWork.ChatRooms.GetByIdAsync(chatRoomId);
            await chatNotificationService.NotifyNewChatRoomAsync(new List<string> { targetUserId }, chatRoomId, $"You have been added to group {room?.Name}");

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> LeaveGroupAsync(Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;

            var participant = await unitOfWork.ChatParticipants.Query().FirstOrDefaultAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId);
            if (participant == null)
                return Result<bool>.Failure(new Error(ErrorCodes.Failure, "ChatRoom or User are not found"));

            unitOfWork.ChatParticipants.Remove(participant);
            await unitOfWork.SaveChangesAsync();

            await cache.RemoveAsync($"chat:inbox:{currentUserId}");

            return Result<bool>.Success(true);
        }
        public async Task<Result<bool>> RemoveMemberFromGroupAsync(string targetUserId, Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;

            // 1. Logic check quyền: Admin mới được xóa người khác. 
            var requesterInfo = await unitOfWork.ChatParticipants.Query()
                .Where(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId)
                .Select(cp => new { cp.IsAdmin })
                .FirstOrDefaultAsync();

            if (requesterInfo == null || !requesterInfo.IsAdmin)
            {
                return Result<bool>.Failure(new Error(ErrorCodes.Failure, "chatroom not found or user not an admin"));
            }

            if (currentUserId == targetUserId) return Result<bool>.Failure(new Error(ErrorCodes.Failure,"currentUser is targetUser"));

            // 2. Tìm thành viên cần xóa
            var targetParticipant = await unitOfWork.ChatParticipants.Query()
                .FirstOrDefaultAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == targetUserId);

            if (targetParticipant == null) return Result<bool>.Failure(new Error(ErrorCodes.Failure, "chatroom or user not found"));

            unitOfWork.ChatParticipants.Remove(targetParticipant);
            await unitOfWork.SaveChangesAsync();

            // 3. Quan trọng: Invalidate Cache cho người bị xóa
            await cache.RemoveAsync($"chat:inbox:{targetUserId}");

            return Result<bool>.Success(true);
        }

        public async Task<Result<Guid>> CreateGroupRoomAsync(CreateGroupDto groupDto)
        {
            var currentUserId = currentUser.UserId;
            var nameCurrentUser = await unitOfWork.Users.Query().Where(u => u.Id == currentUserId).Select(u => u.UserName).FirstOrDefaultAsync();
            
            var newChatRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                IsGroupChat = true,
                Name = groupDto.GroupName
            };
            unitOfWork.ChatRooms.Add(newChatRoom);

            var participants = groupDto.MemberIds.Select(memberId => new ChatParticipant
            {
                ChatRoomId = newChatRoom.Id,
                UserId = memberId
            }).ToList();
            //
            participants.Add(new ChatParticipant
            {
                ChatRoomId = newChatRoom.Id,
                UserId = currentUserId,
                IsAdmin = true
            });

            unitOfWork.ChatParticipants.AddRange(participants);
            await unitOfWork.SaveChangesAsync();

            // Thông báo cho tất cả thành viên trong nhóm về phòng chat mới (nếu cần)
            await chatNotificationService.NotifyNewChatRoomAsync(groupDto.MemberIds, newChatRoom.Id, $"{nameCurrentUser} added you to group {newChatRoom.Name}"!);

            return Result<Guid>.Success(newChatRoom.Id);
        }

        public async Task<Result<Guid>> GetOrCreatePrivateRoomAsync(string targetUserId)
        {
            var currentUserId = currentUser.UserId;
            var nameCurrentUser = unitOfWork.Users.Query().Where(u => u.Id == currentUserId).Select(u => u.UserName).FirstOrDefault();

            if (currentUserId == targetUserId)
                return Result<Guid>.Failure(new Error(ErrorCodes.Failure, "currentUser is targerUser"));

            // Kiểm tra xem đã có phòng chat riêng giữa 2 người chưa
            var existingChatRoom = await unitOfWork.ChatRooms.Query()
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
            unitOfWork.ChatRooms.Add(newChatRoom);

            unitOfWork.ChatParticipants.AddRange(new[]
            {
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = currentUserId },
                new ChatParticipant { ChatRoomId = newChatRoom.Id, UserId = targetUserId }
            });

            await unitOfWork.SaveChangesAsync();

            // Thông báo cho người dùng liên quan về phòng chat mới (nếu cần)
            // Chúng ta dùng Clients.User(targetUserId) để bắn tin cho đúng người đó
            // SignalR sẽ tự tìm tất cả các ConnectionId của User B để gửi
            await chatNotificationService.NotifyNewChatRoomPrivateAsync(targetUserId, newChatRoom.Id, nameCurrentUser!);

            return Result<Guid>.Success(newChatRoom.Id);
        }

        public async Task<Result<CursorPagedResponse<MessageDto>>> GetMessagesRoomAsync(Guid chatRoomId, CursorPaginationRequest messagePagi)
        {
            try
            {
                var currentUserId = currentUser.UserId;
                var isMember = await unitOfWork.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId);

                if (!isMember)
                    return Result<CursorPagedResponse<MessageDto>>.Failure(new Error(ErrorCodes.Failure, "user not a member"));

                var query = unitOfWork.Messages.Query();

                if (messagePagi.Cursor.HasValue)
                {
                    query = query.Where(m => m.CreatedAt < messagePagi.Cursor.Value);
                }

                var messages = await query
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(messagePagi.PageSize + 1)
                    .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                var hasNextMessage = messages.Count > messagePagi.PageSize;
                DateTime? nextCursor = null;

                if (hasNextMessage)
                {
                    messages.RemoveAt(messagePagi.PageSize);
                    nextCursor = messages.Last().CreatedAt;
                }

                var mess = new CursorPagedResponse<MessageDto>
                {
                    Items = messages,
                    HasNextPage = hasNextMessage,
                    NextCursor = nextCursor,
                };
               
                if(nextCursor == null)
                {
                    await MarkRoomAsReadAsync(chatRoomId);
                }

                return Result<CursorPagedResponse<MessageDto>>.Success(mess);
            }
            catch (Exception)
            {
                // Log lỗi nếu cần
                return Result<CursorPagedResponse<MessageDto>>.Failure(new Error(ErrorCodes.Failure, "An error occurred while retrieving messages."));
            }
        }

        // Lấy danh sách phòng chat của người dùng với phân trang
        public async Task<Result<List<ChatRoomDto>>> GetUserChatRoomsAsync()
        {
            var currentUserId = currentUser.UserId;
            string cacheKey = $"chat:inbox:{currentUserId}";

            var rooms = await cache.GetOrCreateAsync(
                cacheKey,
                factory: async () =>
                {
                    return await unitOfWork.ChatRooms.Query()
                        .Where(cr => cr.ChatParticipant.Any(cp => cp.UserId == currentUserId))
                        .ProjectTo<ChatRoomDto>(mapper.ConfigurationProvider, new { currentUserId = currentUserId })
                        .OrderByDescending(cr => cr.LastestMessageAt)
                        .ToListAsync();
                },
                TimeSpan.FromSeconds(30) // Cache ngắn vì Inbox cần cập nhật nhanh
            );

            return Result<List<ChatRoomDto>>.Success(rooms);
        }

        
        public async Task<Result<bool>> MarkRoomAsReadAsync(Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;

            var rowsAffected = await unitOfWork.ChatParticipants.Query()
                .Where(m => m.ChatRoomId == chatRoomId && m.UserId == currentUserId)
                .ExecuteUpdateAsync(ex => ex.SetProperty(cp => cp.LastReadAt, DateTime.UtcNow));

            if (rowsAffected > 0)
            {
                // Xóa cache inbox để cập nhật lại số UnreadMessagesCount
                await cache.RemoveAsync($"chat:inbox:{currentUserId}");
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }

        public async Task<Result<MessageDto>> CreateMessageAsync(SendMessageDto request)
        {
            var currentUserId = currentUser.UserId;

            // 1. Kiểm tra xem người gửi có trong phòng không (Bảo mật)
            var participant = await unitOfWork.ChatParticipants.Query()
                .FirstOrDefaultAsync(cp => cp.ChatRoomId == request.ChatRoomId && cp.UserId == currentUserId);

            if (participant == null)
                return Result<MessageDto>.Failure(new Error(ErrorCodes.Forbid, "You do not have permission to message in this room."));

            // 2. Tạo đối tượng tin nhắn mới
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatRoomId = request.ChatRoomId,
                SenderId = currentUserId,
                Content = request.Content,
                Type = request.Type,
                MediaUrl = request.MediaUrl,
                ReplyToMessageId = request.ReplyToMessageId,
                CreatedAt = DateTime.UtcNow
            };

            unitOfWork.Messages.Add(message);

            // 3. Cập nhật mốc LastReadAt cho chính người gửi (vì mình gửi thì coi như đã xem)
            participant.LastReadAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();

            // 4. Cập nhật Cache Inbox (Như chúng ta đã bàn - cộng dồn số tin chưa đọc cho người khác)
            // Sau khi lưu xong, Mạnh gọi hàm UpdateInboxCacheAsync cho những người còn lại trong phòng nhé

            // 5. Trả về DTO để Controller bắn qua SignalR
            var messdto = new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = currentUser.UserName,
                Content = message.Content!,
                CreatedAt = message.CreatedAt,
                Type = message.Type,
                MediaUrl = message.MediaUrl,
            };

            await chatNotificationService.NotifyReceiveMessageAsync(message.ChatRoomId, messdto);

            return Result<MessageDto>.Success(messdto);
        }

        public async Task<Result<MessageDto>> UploadMessageMediaAsync(IFormFile file, Guid chatRoomId)
        {
            var currentUserId = currentUser.UserId;
            var isMember = await unitOfWork.ChatParticipants.AnyAsync(cp => cp.ChatRoomId == chatRoomId && cp.UserId == currentUserId);
            if (!isMember) return Result<MessageDto>.Failure(new Error(ErrorCodes.Failure, "User not a member"));

            // 1. Upload ảnh
            var uploadResult = await storageServices.UploadImageAsync(file, currentUserId, "image-chat", 400, 400);
            if (!uploadResult.IsSuccess) return Result<MessageDto>.Failure(new Error(ErrorCodes.Failure, "Upload failed"));

            // 2. Tạo Request
            var request = new SendMessageDto
            {
                ChatRoomId = chatRoomId,
                Type = MessageType.Image,
                MediaUrl = uploadResult.Value,
                Content = "[Image]"
            };

            // 3. Gọi hàm lưu
            var result = await CreateMessageAsync(request);

            if (result.IsSuccess)
            {
                return result;
            }

            return Result<MessageDto>.Failure(new Error(ErrorCodes.Failure, "Upload failed"));
        }

        public async Task<Result<bool>> UnsendMessageAsync(Guid messageId )
        {
            var currentUserId = currentUser.UserId;

            // 1. Tìm tin nhắn và kiểm tra quyền chủ sở hữu
            var message = await unitOfWork.Messages.Query()
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == currentUserId);

            if (message == null)
                return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Message not found or you do not have permission to unsend."));

            if (message.CreatedAt < DateTime.UtcNow.AddDays(-1))
            {
                return Result<bool>.Failure(new Error(ErrorCodes.Forbid, "Unsend time limit expired"));
            }

            // 2. Đánh dấu xóa (Soft Delete)
            message.IsDeleted = true;
            message.Content = "Message unsent"; // Ghi đè nội dung để bảo mật
            message.MediaUrl = null; // Xóa link ảnh/video nếu có

            await unitOfWork.SaveChangesAsync();

            // 3. Xóa Cache Inbox (Để người khác thấy tin nhắn cuối cùng đã bị thu hồi)
            // Bạn có thể gọi hàm UpdateInboxCacheAsync tại đây nếu muốn

            await chatNotificationService.NotifyMessageUnsentAsync(message.ChatRoomId, messageId);
            return Result<bool>.Success(true);
        }

        public async Task<Result<MessageReaction>> ReactToMessageAsync(Guid messageId, string emoji)
        {
            var currentUserId = currentUser.UserId;

            // 1. Kiểm tra xem tin nhắn có tồn tại không
            var message = await unitOfWork.Messages.Query().FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return Result<MessageReaction>.Failure(new Error(ErrorCodes.NotFound, "Message does not exist."));
            // 2. Kiểm tra xem User này đã từng thả cảm xúc vào tin này chưa
            var existingReaction = await unitOfWork.MessageReactions.Query()
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == currentUserId);

            if (existingReaction != null)
            {
                if (existingReaction.Emoji == emoji)
                {
                    // Nếu bấm lại đúng Emoji cũ -> Xóa (Toggle off)
                    unitOfWork.MessageReactions.Remove(existingReaction);
                }
                else
                {
                    // Nếu bấm Emoji khác -> Cập nhật cái mới
                    existingReaction.Emoji = emoji;
                }
            }
            else
            {
                // 3. Chưa có thì tạo mới
                existingReaction = new MessageReaction
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId,
                    MessageId = messageId,
                    Emoji = emoji
                };
                unitOfWork.MessageReactions.Add(existingReaction);
            }

            await unitOfWork.SaveChangesAsync();

            await chatNotificationService.NotifyMessageReactedAsync(message.ChatRoomId, messageId, currentUserId, emoji);
            return Result<MessageReaction>.Success(existingReaction);
        }
    }
}
