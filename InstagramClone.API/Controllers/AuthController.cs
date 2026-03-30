using InstagramClone.Application.DTOs.Auth;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class AuthController(IAuthServices userServices) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredUserDto>> Register([FromBody] RegisterUserDto registerUserDto)
    {
        var result = await userServices.RegisterAsync(registerUserDto);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginUserDto loginDto)
    {
        var result = await userServices.LoginAsync(loginDto);
        return ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(TokenResponseDto tokenResponseDto)
    {
        var result = await userServices.RefreshTokenAsync(tokenResponseDto);
        return ToActionResult(result);
    }
}

