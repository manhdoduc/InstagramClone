using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Application.Interfaces.Notifications;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Services
{
    public class SocialNotificationService(IHubContext<NotificationHub, INotificationHub> hubContext) : ISocialNotificationService
    {
        public async Task SendNotificationAsync(string recipientUserId, NotificationDto notification)
        {
            await hubContext.Clients.User(recipientUserId).ReceiveNotification(notification);
        }

        public async Task SendUnreadCountUpdateAsync(string recipientUserId, int unreadCount)
        {
            await hubContext.Clients.User(recipientUserId).UnreadCountUpdated(unreadCount);
        }
    }
}
