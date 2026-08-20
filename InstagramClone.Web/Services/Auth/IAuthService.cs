using InstagramClone.Web.Models.Auth;
using InstagramClone.Web.Models.Common;

namespace InstagramClone.Web.Services.Auth;

public interface IAuthService
{
    Task<ApiResult<TokenResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<RegisteredUser>> RegisterAsync(RegisterRequest request);
    Task<ApiResult> LogoutAsync();
    Task<ApiResult<TokenResponse>> RefreshTokenAsync(TokenResponse tokens);
}
