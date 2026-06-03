using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InstagramClone.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("LoginLimit")]
public class AuthController(IAuthServices userServices) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredUserDto>> Register([FromBody] RegisterUserDto registerUserDto)
    {
        var result = await userServices.RegisterAsync(registerUserDto);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginUserDto loginDto)
    {
        var result = await userServices.LoginAsync(loginDto);
        return ToActionResult(result);
    }

    [AllowAnonymous] // Thường Refresh Token không cần header Authorize vì bản thân nó đã gửi token cũ lên rồi
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] TokenResponseDto tokenResponseDto) // Nên thêm [FromBody] để đồng bộ
    {
        var result = await userServices.RefreshTokenAsync(tokenResponseDto);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<bool>> Logout()
    {
        var result = await userServices.LogoutAsync();
        return ToActionResult(result);
    }
}