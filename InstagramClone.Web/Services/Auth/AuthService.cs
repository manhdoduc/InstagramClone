using InstagramClone.Web.Authentication;
using InstagramClone.Web.Models.Auth;
using InstagramClone.Web.Models.Common;

namespace InstagramClone.Web.Services.Auth;

/// <summary>
/// Service xử lý Authentication — Login, Register, Logout, RefreshToken.
/// HttpClient được inject với AuthenticationDelegatingHandler (tự động attach Bearer token).
/// Riêng Login/Register/Refresh dùng trực tiếp, không cần token.
/// </summary>
public class AuthService(HttpClient httpClient, TokenStorageService tokenStorage, CustomAuthStateProvider authStateProvider)
    : BaseApiService(httpClient), IAuthService
{
    public async Task<ApiResult<TokenResponse>> LoginAsync(LoginRequest request)
    {
        var result = await PostAsync<TokenResponse>("/api/auth/login", new
        {
            identifier = request.Email,
            password = request.Password
        });

        if (result.IsSuccess && result.Data != null)
        {
            // Lưu vào memory ngay lập tức (không cần chờ JS Interop)
            tokenStorage.SetTokensInMemory(result.Data.AccessToken, result.Data.RefreshToken);
            authStateProvider.NotifyUserAuthenticated(result.Data.AccessToken);
            // Sau đó persist vào localStorage
            await tokenStorage.SaveTokensAsync(result.Data);
        }

        return result;
    }

    public async Task<ApiResult<RegisteredUser>> RegisterAsync(RegisterRequest request)
    {
        return await PostAsync<RegisteredUser>("/api/auth/register", new
        {
            firstName = request.FirstName,
            lastName = request.LastName,
            nickName = request.UserName,
            email = request.Email,
            password = request.Password
        });
    }

    public async Task<ApiResult> LogoutAsync()
    {
        // Xóa token trong memory ngay lập tức trước khi gọi API
        tokenStorage.ClearTokensInMemory();
        authStateProvider.NotifyUserLoggedOut();
        var result = await PostAsync("/api/auth/logout");
        await tokenStorage.ClearTokensAsync();
        return result;
    }

    public async Task<ApiResult<TokenResponse>> RefreshTokenAsync(TokenResponse tokens)
    {
        var result = await PostAsync<TokenResponse>("/api/auth/refresh-token", tokens);

        if (result.IsSuccess && result.Data != null)
        {
            await tokenStorage.SaveTokensAsync(result.Data);
            authStateProvider.NotifyUserAuthenticated(result.Data.AccessToken);
        }

        return result;
    }
}
