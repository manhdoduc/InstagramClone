using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Chat;
using InstagramClone.Web.Models.Common;

namespace InstagramClone.Web.Services.Chat;

public interface IChatService
{
    Task<ApiResult<List<ChatRoomDto>>> GetUserChatRoomsAsync();
    Task<ApiResult<Guid>> GetOrCreatePrivateRoomAsync(string targetUserId);
    Task<ApiResult<Guid>> CreateGroupRoomAsync(CreateGroupRequest request);
    Task<ApiResult<CursorPagedResponse<MessageDto>>> GetMessagesAsync(Guid roomId, PaginationRequest request);
    Task<ApiResult<MessageDto>> SendMessageAsync(SendMessageRequest request);
    Task<ApiResult<MessageDto>> UploadMediaAsync(Guid roomId, IBrowserFile file);
    Task<ApiResult> ReactToMessageAsync(Guid messageId, string emoji);
    Task<ApiResult<bool>> UnsendMessageAsync(Guid messageId);
}
