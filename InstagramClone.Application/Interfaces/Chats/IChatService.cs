using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
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

 

        // L?y tin nh?n c?a m?t phòng chat v?i phân trang
        Task<Result<CursorPagedResponse<MessageDto>>> GetMessagesRoomAsync(Guid chatRoomId, CursorPaginationRequest messagePagi);

        // l?y danh sách phòng chat c?a ngu?i dùng v?i phân trang
        Task<Result<List<ChatRoomDto>>> GetUserChatRoomsAsync();

        // dánh d?u t?t c? tin nh?n trong phòng chat là dã d?c
        Task<Result<bool>> MarkRoomAsReadAsync(Guid chatRoomId);

        // g?i ?nh 
        Task<Result<MessageDto>> UploadMessageMediaAsync(IFormFile file, Guid chatRoomId);
        Task<Result<MessageDto>> CreateMessageAsync(SendMessageDto request);

        Task<Result<bool>> UnsendMessageAsync(Guid chatRoomId);
        Task<Result<MessageReaction>> ReactToMessageAsync(Guid messageId, string emoji);
    }
}
