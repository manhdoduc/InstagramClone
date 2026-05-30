using InstagramClone.Application.DTOs.Auth;
using InstagramClone.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services
{
    public interface IAuthServices
    {
        Task<Result<TokenResponseDto>> LoginAsync(LoginUserDto loginDto);
        Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto);
        Task<Result<TokenResponseDto>> RefreshTokenAsync(TokenResponseDto tokenResponseDto);
        Task<Result<bool>> LogoutAsync();
    }
}
