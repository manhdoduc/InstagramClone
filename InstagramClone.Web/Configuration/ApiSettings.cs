namespace InstagramClone.Web.Configuration;

/// <summary>
/// Cấu hình Base URL và các endpoint của backend API.
/// Được đọc từ appsettings.json section "ApiSettings".
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5063";
    public string ChatHubUrl => $"{BaseUrl}/chathub";
    public string NotificationHubUrl => $"{BaseUrl}/hubs/notifications";
}
