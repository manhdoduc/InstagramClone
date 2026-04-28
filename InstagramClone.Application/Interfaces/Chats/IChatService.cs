using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Chats
{
    public interface IChatService
    {
        Task<Result<Guid>> GetOrCreatePrivateRoomAsync(string targetUserId);
        Task<Result<Guid>> CreateGroupRoomAsync(CreateGroupDto groupDto);

        Task<Result<bool>> AddMemberToGroupAsync(string targetUserId, Guid chatRoomId);

        Task<Result<bool>> RemoveMemberFromGroupAsync(string targetUserId ,Guid chatRoomId);

        Task<Result<bool>> LeaveGroupAsync(Guid chatRoomId);

 

        // Lấy tin nhắn của một phòng chat với phân trang
        Task<Result<CursorPagedResponse<MessageDto>>> GetMessagesRoomAsync(Guid chatRoomId, CursorPaginationRequest messagePagi);

        // lấy danh sách phòng chat của người dùng với phân trang
        Task<Result<List<ChatRoomDto>>> GetUserChatRoomsAsync();

        // đánh dấu tất cả tin nhắn trong phòng chat là đã đọc
        Task<Result<bool>> MarkRoomAsReadAsync(Guid chatRoomId);

        // gửi ảnh 
        Task<Result<MessageDto>> UploadMessageMediaAsync(IFormFile file, Guid chatRoomId);
        Task<Result<MessageDto>> CreateMessageAsync(SendMessageDto request);

        Task<Result<bool>> UnsendMessageAsync(Guid chatRoomId);
        Task<Result<MessageReaction>> ReactToMessageAsync(Guid messageId, string emoji);
    }
}
