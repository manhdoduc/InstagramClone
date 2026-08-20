using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Notifications;

namespace InstagramClone.Web.State;

/// <summary>
/// App-level state được share qua CascadingValue.
/// Lưu các dữ liệu cần realtime update toàn app: unread notification count, online users...
/// 
/// Quyết định thiết kế: Dùng event-based notification thay vì Fluxor/Redux
/// để tránh over-engineering — phù hợp với quy mô project hiện tại.
/// </summary>
public class AppState
{
    // Unread notification count (realtime từ SignalR NotificationHub)
    private int _unreadNotificationCount;
    public int UnreadNotificationCount
    {
        get => _unreadNotificationCount;
        private set
        {
            _unreadNotificationCount = value;
            OnStateChanged?.Invoke();
        }
    }

    // Danh sách user đang online (realtime từ SignalR ChatHub)
    public HashSet<string> OnlineUserIds { get; private set; } = new();

    // Event để notify components re-render
    public event Action? OnStateChanged;

    public void SetUnreadCount(int count)
    {
        UnreadNotificationCount = count;
    }

    public void IncrementUnreadCount()
    {
        UnreadNotificationCount++;
    }

    public void MarkAllRead()
    {
        UnreadNotificationCount = 0;
    }

    public void SetUserOnline(string userId)
    {
        OnlineUserIds.Add(userId);
        OnStateChanged?.Invoke();
    }

    public void SetUserOffline(string userId)
    {
        OnlineUserIds.Remove(userId);
        OnStateChanged?.Invoke();
    }

    public bool IsUserOnline(string userId)
        => OnlineUserIds.Contains(userId);
}
