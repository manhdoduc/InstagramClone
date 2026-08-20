using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InstagramClone.Web.Configuration;
using InstagramClone.Web.Models.Auth;
using Microsoft.Extensions.Options;

namespace InstagramClone.Web.Authentication;

/// <summary>
/// DelegatingHandler tự động:
/// 1. Inject "Authorization: Bearer {token}" vào mọi outgoing request.
/// 2. Khi nhận được 401, tự động gọi /api/auth/refresh-token.
/// 3. Nếu refresh thành công → retry request gốc với token mới.
/// 4. Nếu refresh thất bại → xóa token và để 401 propagate (redirect về login).
/// 
/// Quyết định thiết kế: Dùng HttpMessageHandler thay vì interceptor ở mỗi service
/// để đảm bảo DRY — chỉ viết logic refresh 1 lần cho toàn bộ HttpClient.
/// </summary>
public class AuthenticationDelegatingHandler(
    TokenStorageService tokenStorage,
    IOptions<ApiSettings> apiSettings) : DelegatingHandler
{
    private readonly ApiSettings _apiSettings = apiSettings.Value;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static bool _isRefreshing = false;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Bỏ qua các endpoint auth (login, register, refresh)
        if (IsAuthEndpoint(request.RequestUri))
            return await base.SendAsync(request, cancellationToken);

        // Gắn token vào request
        var accessToken = await tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = accessToken.Trim('"').Trim();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        Console.WriteLine($"[DelegatingHandler] Request: {request.Method} {request.RequestUri} | HasToken: {!string.IsNullOrWhiteSpace(accessToken)} | TokenSnippet: {(string.IsNullOrWhiteSpace(accessToken) ? "NONE" : accessToken.Substring(0, Math.Min(20, accessToken.Length)))}");

        var response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine($"[DelegatingHandler] Response: {request.RequestUri} -> {(int)response.StatusCode} {response.StatusCode}");

        // Nếu 401 → thử refresh token
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var newToken = await TryRefreshTokenAsync(cancellationToken);
            if (newToken != null)
            {
                // Clone request và retry với token mới
                var retryRequest = await CloneHttpRequestAsync(request, newToken);
                response = await base.SendAsync(retryRequest, cancellationToken);
                Console.WriteLine($"[DelegatingHandler] Retry Response: {request.RequestUri} -> {(int)response.StatusCode} {response.StatusCode}");
            }
        }

        return response;
    }

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_isRefreshing) return null;
            _isRefreshing = true;

            var refreshToken = await tokenStorage.GetRefreshTokenAsync();
            var accessToken = await tokenStorage.GetAccessTokenAsync();

            refreshToken = refreshToken?.Trim('"').Trim();
            accessToken = accessToken?.Trim('"').Trim();

            if (string.IsNullOrWhiteSpace(refreshToken)) return null;

            var payload = new { accessToken, refreshToken };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiSettings.BaseUrl}/api/auth/refresh-token")
            {
                Content = content
            };

            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode)
            {
                if (refreshResponse.StatusCode == HttpStatusCode.Unauthorized || refreshResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    await tokenStorage.ClearTokensAsync();
                }
                return null;
            }

            var json = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null) return null;

            await tokenStorage.SaveTokensAsync(tokenResponse);
            return tokenResponse.AccessToken?.Trim('"').Trim();
        }
        finally
        {
            _isRefreshing = false;
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage request, string newToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copy headers (trừ Authorization cũ)
        foreach (var header in request.Headers)
        {
            if (header.Key != "Authorization")
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        // Copy content nếu có
        if (request.Content != null)
        {
            var bodyBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bodyBytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private static bool IsAuthEndpoint(Uri? uri)
    {
        if (uri == null) return false;
        var path = uri.AbsolutePath.ToLower();
        return path.EndsWith("/api/auth/login") 
            || path.EndsWith("/api/auth/register") 
            || path.EndsWith("/api/auth/refresh-token");
    }
}
