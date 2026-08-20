using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InstagramClone.Web.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace InstagramClone.Web.Authentication;

/// <summary>
/// Custom AuthenticationStateProvider cho Blazor Server.
/// Parse JWT Claims từ access token lưu trong localStorage để cung cấp AuthenticationState.
/// 
/// Quyết định thiết kế: Dùng JwtSecurityTokenHandler để parse claims từ token ở client-side,
/// tránh round-trip đến API chỉ để lấy thông tin user hiện tại.
/// </summary>
public class CustomAuthStateProvider(TokenStorageService tokenStorage) : AuthenticationStateProvider
{
    private readonly JwtSecurityTokenHandler _jwtHandler = new();
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _cachedUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            if (_cachedUser != null && _cachedUser.Identity?.IsAuthenticated == true)
            {
                return new AuthenticationState(_cachedUser);
            }

            var accessToken = await tokenStorage.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(accessToken))
                return new AuthenticationState(_anonymous);

            var claims = ParseClaimsFromJwt(accessToken);
            if (claims == null)
                return new AuthenticationState(_anonymous);

            // Kiểm tra token hết hạn
            var expClaim = claims.FindFirst("exp");
            if (expClaim != null)
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
                if (expDate <= DateTimeOffset.UtcNow)
                {
                    _cachedUser = null;
                    return new AuthenticationState(_anonymous);
                }
            }

            var identity = new ClaimsIdentity(claims.Claims, "jwt");
            _cachedUser = new ClaimsPrincipal(identity);
            return new AuthenticationState(_cachedUser);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    /// <summary>Gọi sau khi login/register thành công để notify toàn bộ app.</summary>
    public void NotifyUserAuthenticated(string accessToken)
    {
        var claims = ParseClaimsFromJwt(accessToken);
        if (claims == null) return;

        var identity = new ClaimsIdentity(claims.Claims, "jwt");
        _cachedUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }

    /// <summary>Gọi sau khi logout để reset toàn bộ auth state.</summary>
    public void NotifyUserLoggedOut()
    {
        _cachedUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    /// <summary>Lấy CurrentUser từ auth state hiện tại.</summary>
    public async Task<CurrentUser> GetCurrentUserAsync()
    {
        var state = await GetAuthenticationStateAsync();
        var principal = state.User;

        if (!principal.Identity?.IsAuthenticated ?? true)
            return new CurrentUser();

        return new CurrentUser
        {
            Id = principal.FindFirst("sub")?.Value
              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? string.Empty,
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            UserName = principal.FindFirst("unique_name")?.Value
                    ?? principal.FindFirst(ClaimTypes.Name)?.Value
                    ?? string.Empty,
            Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            IsAuthenticated = true
        };
    }

    private ClaimsPrincipal? ParseClaimsFromJwt(string token)
    {
        try
        {
            if (!_jwtHandler.CanReadToken(token)) return null;
            var jwtToken = _jwtHandler.ReadJwtToken(token);
            return new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims, "jwt"));
        }
        catch
        {
            return null;
        }
    }
}
