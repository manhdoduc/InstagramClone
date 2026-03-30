using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bắt buộc đăng nhập
public class UsersController(IUserServices userServices) : BaseApiController
{
    [HttpPut("avatar")]
    public async Task<ActionResult<string>> UploadAvatar([FromForm] UploadImageDto uploadImageDto)
    {
        var result = await userServices.UploadAvatarAsync(uploadImageDto.File);
        return ToActionResult(result);
    }

    [HttpGet("{userId}/avatar")]
    public async Task<ActionResult<string>> GetAvatarUrl(string userId)
    {
        var result = await userServices.GetAvatarUrlAsync(userId);
        return ToActionResult(result);
    }

    [HttpDelete("avatar")]
    public async Task<ActionResult<bool>> DeleteAccount()
    {
        var result = await userServices.DeleteAvatarAsync();
        return ToActionResult(result);
    }

    [HttpPut("privacy/toggle")]
    public async Task<ActionResult<string>> ToggleAccountPrivacy()
    {
        var result = await userServices.ToggleAccountPrivacyAsync();
        return ToActionResult(result);
    }

    [HttpGet("{targetUserId}")]
    public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile(string targetUserId)
    {
        var result = await userServices.GetUserProfileAsync(targetUserId);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserSummaryDto>>> SearchUsers([FromQuery] string query)
    {
        var result = await userServices.SearchUsersAsync(query);
        return ToActionResult(result);
    }
}

