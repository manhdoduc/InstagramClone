using Blazored.LocalStorage;
using InstagramClone.Web.Models.Auth;

namespace InstagramClone.Web.Authentication;

/// <summary>
/// Lưu trữ và đọc Access/Refresh token.
/// Dùng in-memory cache làm lớp chính (luôn hoạt động trong Blazor Server circuit).
/// localStorage là lớp thứ hai để persist qua page reload (chỉ đọc/ghi khi JS Interop sẵn sàng).
/// </summary>
public class TokenStorageService(ILocalStorageService localStorage)
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    // In-memory cache — luôn sẵn sàng dù JS Interop chưa khởi tạo
    private string? _cachedAccessToken;
    private string? _cachedRefreshToken;

    public async Task SaveTokensAsync(TokenResponse tokens)
    {
        var cleanAccess = tokens.AccessToken?.Trim('"').Trim() ?? "";
        var cleanRefresh = tokens.RefreshToken?.Trim('"').Trim() ?? "";

        // Lưu vào memory trước
        _cachedAccessToken = cleanAccess;
        _cachedRefreshToken = cleanRefresh;

        // Sau đó lưu vào localStorage (không blocking nếu JS Interop fail)
        try
        {
            await localStorage.SetItemAsStringAsync(AccessTokenKey, cleanAccess);
            await localStorage.SetItemAsStringAsync(RefreshTokenKey, cleanRefresh);
        }
        catch { }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        // 1. Ưu tiên in-memory cache
        if (!string.IsNullOrWhiteSpace(_cachedAccessToken))
        {
            Console.WriteLine($"[TokenStorage] GetAccessToken -> Loaded from Memory Cache: '{_cachedAccessToken.Substring(0, Math.Min(15, _cachedAccessToken.Length))}...'");
            return _cachedAccessToken;
        }

        // 2. Fallback: đọc từ localStorage (khi page reload, circuit mới)
        try
        {
            var token = await localStorage.GetItemAsStringAsync(AccessTokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await localStorage.GetItemAsync<string>(AccessTokenKey);
            }
            token = token?.Trim('"').Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                _cachedAccessToken = token;
                Console.WriteLine($"[TokenStorage] GetAccessToken -> Loaded from LocalStorage: '{token.Substring(0, Math.Min(15, token.Length))}...'");
                return token;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenStorage] GetAccessToken -> Exception reading LocalStorage: {ex.Message}");
        }

        Console.WriteLine("[TokenStorage] GetAccessToken -> Token is NULL/EMPTY!");
        return null;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        // 1. Ưu tiên in-memory cache
        if (!string.IsNullOrWhiteSpace(_cachedRefreshToken))
            return _cachedRefreshToken;

        // 2. Fallback: đọc từ localStorage
        try
        {
            var token = await localStorage.GetItemAsStringAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await localStorage.GetItemAsync<string>(RefreshTokenKey);
            }
            token = token?.Trim('"').Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                _cachedRefreshToken = token;
                return token;
            }
        }
        catch
        {
            try
            {
                var token = await localStorage.GetItemAsync<string>(RefreshTokenKey);
                token = token?.Trim('"').Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _cachedRefreshToken = token;
                    return token;
                }
            }
            catch { }
        }

        return null;
    }

    public async Task ClearTokensAsync()
    {
        _cachedAccessToken = null;
        _cachedRefreshToken = null;

        try
        {
            await localStorage.RemoveItemAsync(AccessTokenKey);
            await localStorage.RemoveItemAsync(RefreshTokenKey);
        }
        catch { }
    }

    /// <summary>
    /// Đặt token vào in-memory cache mà không đụng đến localStorage.
    /// Dùng ngay sau khi parse token từ JWT (không cần await JS Interop).
    /// </summary>
    public void SetTokensInMemory(string accessToken, string refreshToken)
    {
        _cachedAccessToken = accessToken.Trim('"').Trim();
        _cachedRefreshToken = refreshToken.Trim('"').Trim();
    }

    public void ClearTokensInMemory()
    {
        _cachedAccessToken = null;
        _cachedRefreshToken = null;
    }
}
