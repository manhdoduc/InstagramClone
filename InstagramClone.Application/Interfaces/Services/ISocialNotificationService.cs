using InstagramClone.Application.Features.Notifications.DTOs;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services
{
    public interface ISocialNotificationService
    {
        Task SendNotificationAsync(string recipientUserId, NotificationDto notification);
        Task SendUnreadCountUpdateAsync(string recipientUserId, int unreadCount);
    }
}
