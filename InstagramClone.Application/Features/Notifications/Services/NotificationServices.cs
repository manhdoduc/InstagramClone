using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Notifications.Services
{
    public class NotificationServices(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ISocialNotificationService socialNotificationService,
        ICacheService cache
        ) : INotificationServices
    {
        public async Task CreateAndSendNotificationAsync(Guid recipientId, Guid actorId, NotificationType type, string message, Guid? targetId = null)
        {
            // Do not send notification to oneself
            if (recipientId == actorId) return;

            var notification = new Notification(recipientId, actorId, type, message, targetId);
            unitOfWork.Notifications.Add(notification);
            await unitOfWork.SaveChangesAsync();

            var actor = await unitOfWork.Users.GetByIdAsync(actorId);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                RecipientId = recipientId.ToString(),
                ActorId = actorId.ToString(),
                ActorUsername = actor?.UserName ?? string.Empty,
                ActorAvatarUrl = actor?.AvatarUrl,
                Type = type,
                TargetId = targetId,
                Message = message,
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await cache.RemoveAsync($"notifications:unread:{recipientId}");

            // Push realtime notification
            await socialNotificationService.SendNotificationAsync(recipientId.ToString(), dto);

            var unreadCount = await unitOfWork.Notifications.GetUnreadCountAsync(recipientId);
            await socialNotificationService.SendUnreadCountUpdateAsync(recipientId.ToString(), unreadCount);
        }

        public async Task<Result<CursorPagedResponse<NotificationDto>>> GetUserNotificationsAsync(CursorPaginationRequest request)
        {
            if (!Guid.TryParse(currentUser.UserId, out var userId))
                return Result<CursorPagedResponse<NotificationDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var notifications = await unitOfWork.Notifications.GetUserNotificationsAsync(userId, request.Cursor, request.PageSize);
            return Result<CursorPagedResponse<NotificationDto>>.Success(notifications);
        }

        public async Task<Result<int>> GetUnreadCountAsync()
        {
            if (!Guid.TryParse(currentUser.UserId, out var userId))
                return Result<int>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            string cacheKey = $"notifications:unread:{userId}";
            var count = await cache.GetOrCreateAsync(
                cacheKey,
                factory: async () => await unitOfWork.Notifications.GetUnreadCountAsync(userId),
                TimeSpan.FromSeconds(30)
            );

            return Result<int>.Success(count);
        }

        public async Task<Result<bool>> MarkAsReadAsync(Guid notificationId)
        {
            if (!Guid.TryParse(currentUser.UserId, out var userId))
                return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var notification = await unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null || notification.RecipientId != userId)
                return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Notification not found"));

            if (!notification.IsRead)
            {
                notification.MarkAsRead();
                await unitOfWork.SaveChangesAsync();
                await cache.RemoveAsync($"notifications:unread:{userId}");

                var unreadCount = await unitOfWork.Notifications.GetUnreadCountAsync(userId);
                await socialNotificationService.SendUnreadCountUpdateAsync(userId.ToString(), unreadCount);
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> MarkAllAsReadAsync()
        {
            if (!Guid.TryParse(currentUser.UserId, out var userId))
                return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            await unitOfWork.Notifications.MarkAllAsReadAsync(userId);
            await cache.RemoveAsync($"notifications:unread:{userId}");

            await socialNotificationService.SendUnreadCountUpdateAsync(userId.ToString(), 0);
            return Result<bool>.Success(true);
        }
    }
}
