using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Notifications;

namespace InstagramClone.Web.Services.Notifications;

public interface INotificationService
{
    Task<ApiResult<CursorPagedResponse<NotificationDto>>> GetNotificationsAsync(PaginationRequest request);
    Task<ApiResult<int>> GetUnreadCountAsync();
    Task<ApiResult<bool>> MarkAsReadAsync(Guid id);
    Task<ApiResult<bool>> MarkAllAsReadAsync();
}
