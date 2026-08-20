using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using InstagramClone.Web.Authentication;
using InstagramClone.Web.Configuration;
using InstagramClone.Web.Models.Notifications;
using InstagramClone.Web.State;

namespace InstagramClone.Web.Services.SignalR;

public class NotificationHubService : IAsyncDisposable
{
    private readonly ApiSettings _apiSettings;
    private readonly TokenStorageService _tokenStorage;
    private readonly AppState _appState;
    private HubConnection? _hubConnection;

    public event Action<NotificationDto>? OnNotificationReceived;

    public NotificationHubService(IOptions<ApiSettings> apiSettings, TokenStorageService tokenStorage, AppState appState)
    {
        _apiSettings = apiSettings.Value;
        _tokenStorage = tokenStorage;
        _appState = appState;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        if (_hubConnection == null)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_apiSettings.NotificationHubUrl, options =>
                {
                    options.AccessTokenProvider = async () => await _tokenStorage.GetAccessTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterCallbacks();
        }

        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                var token = await _tokenStorage.GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(token)) return;

                await _hubConnection.StartAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationHubService] Error starting SignalR connection: {ex.Message}");
        }
    }

    private void RegisterCallbacks()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<NotificationDto>("ReceiveNotification", notification =>
        {
            _appState.IncrementUnreadCount();
            OnNotificationReceived?.Invoke(notification);
        });

        _hubConnection.On<int>("ReceiveUnreadCountUpdate", unreadCount =>
        {
            _appState.SetUnreadCount(unreadCount);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
