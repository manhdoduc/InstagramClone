using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
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
        Task<Result<Message>> SaveMessageAsync(Guid chatRoomId, string content);

        // Lấy tin nhắn của một phòng chat với phân trang
        Task<Result<List<MessageDto>>> GetRoomMessagesAsync(Guid chatRoomId, CursorPaginationRequestMessage messagePagi);

        // lấy danh sách phòng chat của người dùng với phân trang
        Task<Result<List<ChatRoomDto>>> GetUserChatRoomsAsync();

        // đánh dấu tất cả tin nhắn trong phòng chat là đã đọc
        Task<Result<bool>> MarkRoomAsReadAsync(Guid chatRoomId);
    }
}
