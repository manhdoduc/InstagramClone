using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InstagramClone.Infrastructure.Services
{
    public class CurrentUserServices(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public string UserId => httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;

        public string UserName => httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Name)?.Value?? string.Empty;
    }
}
