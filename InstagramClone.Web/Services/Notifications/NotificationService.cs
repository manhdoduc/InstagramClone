using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Notifications;

namespace InstagramClone.Web.Services.Notifications;

public class NotificationService(HttpClient httpClient) : BaseApiService(httpClient), INotificationService
{
    public async Task<ApiResult<CursorPagedResponse<NotificationDto>>> GetNotificationsAsync(PaginationRequest request)
        => await GetAsync<CursorPagedResponse<NotificationDto>>($"/api/notifications?{request.ToQueryString()}");

    public async Task<ApiResult<int>> GetUnreadCountAsync()
        => await GetAsync<int>("/api/notifications/unread-count");

    public async Task<ApiResult<bool>> MarkAsReadAsync(Guid id)
        => await PutAsync<bool>($"/api/notifications/{id}/read");

    public async Task<ApiResult<bool>> MarkAllAsReadAsync()
        => await PutAsync<bool>("/api/notifications/read-all");
}
