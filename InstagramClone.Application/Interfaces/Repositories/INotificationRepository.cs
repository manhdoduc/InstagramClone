using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        void Add(Notification notification);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<CursorPagedResponse<NotificationDto>> GetUserNotificationsAsync(Guid userId, DateTime? cursor, int pageSize);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<int> MarkAllAsReadAsync(Guid userId);
    }
}
