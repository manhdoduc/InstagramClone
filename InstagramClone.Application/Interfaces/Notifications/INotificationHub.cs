using InstagramClone.Application.Features.Notifications.DTOs;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Notifications
{
    public interface INotificationHub
    {
        Task ReceiveNotification(NotificationDto notification);
        Task UnreadCountUpdated(int unreadCount);
    }
}
