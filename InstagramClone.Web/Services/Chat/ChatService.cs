using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Chat;
using InstagramClone.Web.Models.Common;

namespace InstagramClone.Web.Services.Chat;

public class ChatService(HttpClient httpClient) : BaseApiService(httpClient), IChatService
{
    public async Task<ApiResult<List<ChatRoomDto>>> GetUserChatRoomsAsync()
        => await GetAsync<List<ChatRoomDto>>("/api/chat/rooms");

    public async Task<ApiResult<Guid>> GetOrCreatePrivateRoomAsync(string targetUserId)
        => await PostAsync<Guid>($"/api/chat/private-room/{targetUserId}");

    public async Task<ApiResult<Guid>> CreateGroupRoomAsync(CreateGroupRequest request)
        => await PostAsync<Guid>("/api/chat/group-room", request);

    public async Task<ApiResult<CursorPagedResponse<MessageDto>>> GetMessagesAsync(Guid roomId, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<MessageDto>>($"/api/chat/rooms/{roomId}/messages?{request.ToQueryString()}");

    public async Task<ApiResult<MessageDto>> SendMessageAsync(SendMessageRequest request)
        => await PostAsync<MessageDto>("/api/chat/messages", request);

    public async Task<ApiResult<MessageDto>> UploadMediaAsync(Guid roomId, IBrowserFile file)
    {
        using var form = new MultipartFormDataContent();
        var fileStream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(streamContent, "file", file.Name);

        return await PostFormAsync<MessageDto>($"/api/chat/rooms/{roomId}/media", form);
    }

    public async Task<ApiResult> ReactToMessageAsync(Guid messageId, string emoji)
        => await PostAsync($"/api/chat/messages/{messageId}/react?emoji={Uri.EscapeDataString(emoji)}");

    public async Task<ApiResult<bool>> UnsendMessageAsync(Guid messageId)
        => await DeleteAsync<bool>($"/api/chat/messages/{messageId}");
}
