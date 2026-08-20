using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using InstagramClone.Web.Authentication;
using InstagramClone.Web.Configuration;
using InstagramClone.Web.Models.Chat;
using InstagramClone.Web.State;

namespace InstagramClone.Web.Services.SignalR;

public class ChatHubService : IAsyncDisposable
{
    private readonly ApiSettings _apiSettings;
    private readonly TokenStorageService _tokenStorage;
    private readonly AppState _appState;
    private HubConnection? _hubConnection;

    public event Action<MessageDto>? OnMessageReceived;
    public event Action<string, Guid>? OnUserTyping;
    public event Action<string, Guid>? OnUserStoppedTyping;
    public event Action<string, Guid>? OnMessagesRead;
    public event Action<Guid, string, string>? OnMessageReacted;

    public ChatHubService(IOptions<ApiSettings> apiSettings, TokenStorageService tokenStorage, AppState appState)
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
                .WithUrl(_apiSettings.ChatHubUrl, options =>
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

                // Lấy danh sách user online ban đầu
                var onlineUserIds = await _hubConnection.InvokeAsync<List<string>>("GetOnlineUsers");
                if (onlineUserIds != null)
                {
                    foreach (var userId in onlineUserIds)
                    {
                        _appState.SetUserOnline(userId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHubService] Error starting SignalR connection: {ex.Message}");
        }
    }

    public async Task JoinRoomAsync(Guid chatRoomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinRoom", chatRoomId);
        }
    }

    public async Task StartTypingAsync(Guid chatRoomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StartTyping", chatRoomId);
        }
    }

    public async Task StopTypingAsync(Guid chatRoomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("StopTyping", chatRoomId);
        }
    }

    public async Task MarkMessagesAsReadAsync(Guid chatRoomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("MarkMessagesAsRead", chatRoomId);
        }
    }

    private void RegisterCallbacks()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<string>("UserOnline", userId =>
        {
            _appState.SetUserOnline(userId);
        });

        _hubConnection.On<string>("UserOffline", userId =>
        {
            _appState.SetUserOffline(userId);
        });

        _hubConnection.On<MessageDto>("ReceiveMessage", message =>
        {
            OnMessageReceived?.Invoke(message);
        });

        _hubConnection.On<string, Guid>("UserTyping", (userId, roomId) =>
        {
            OnUserTyping?.Invoke(userId, roomId);
        });

        _hubConnection.On<string, Guid>("UserStoppedTyping", (userId, roomId) =>
        {
            OnUserStoppedTyping?.Invoke(userId, roomId);
        });

        _hubConnection.On<string, Guid>("MessagesRead", (userId, roomId) =>
        {
            OnMessagesRead?.Invoke(userId, roomId);
        });

        _hubConnection.On<Guid, string, string>("MessageReacted", (messageId, userId, emoji) =>
        {
            OnMessageReacted?.Invoke(messageId, userId, emoji);
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
