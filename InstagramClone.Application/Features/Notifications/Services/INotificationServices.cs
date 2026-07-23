using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Notifications.Services
{
    public interface INotificationServices
    {
        Task CreateAndSendNotificationAsync(Guid recipientId, Guid actorId, NotificationType type, string message, Guid? targetId = null);
        Task<Result<CursorPagedResponse<NotificationDto>>> GetUserNotificationsAsync(CursorPaginationRequest request);
        Task<Result<int>> GetUnreadCountAsync();
        Task<Result<bool>> MarkAsReadAsync(Guid notificationId);
        Task<Result<bool>> MarkAllAsReadAsync();
    }
}
