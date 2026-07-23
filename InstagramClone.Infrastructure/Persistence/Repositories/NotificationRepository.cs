using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository(AppDbContext dbContext) : INotificationRepository
    {
        public void Add(Notification notification)
        {
            dbContext.Notifications.Add(notification);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<CursorPagedResponse<NotificationDto>> GetUserNotificationsAsync(Guid userId, DateTime? cursor, int pageSize)
        {
            var query = dbContext.Notifications
                .AsNoTracking()
                .Include(n => n.Actor)
                .Where(n => n.RecipientId == userId);

            if (cursor.HasValue)
            {
                query = query.Where(n => n.CreatedAt < cursor.Value);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(pageSize + 1)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    RecipientId = n.RecipientId.ToString(),
                    ActorId = n.ActorId.ToString(),
                    ActorUsername = n.Actor.UserName,
                    ActorAvatarUrl = n.Actor.AvatarUrl,
                    Type = n.Type,
                    TargetId = n.TargetId,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return PaginationHelper.ToCursorPaged(notifications, pageSize, n => n.CreatedAt);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await dbContext.Notifications
                .AsNoTracking()
                .CountAsync(n => n.RecipientId == userId && !n.IsRead);
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            return await dbContext.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }
}
