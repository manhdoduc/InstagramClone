using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController(IUserServices userServices) : BaseApiController
{
    #region 1. Các Route tinh (Static Routes) và Search - Ð?t lên d?u d? tránh xung d?t

    // GET: api/users/search?query=abc
    [HttpGet("search")]
    public async Task<ActionResult<List<UserSummaryDto>>> SearchUsers([FromQuery] string query)
    {
        var result = await userServices.SearchUsersAsync(query);
        return ToActionResult(result);
    }

    #endregion

    #region 2. Qu?n lý Avatar (?nh d?i di?n)

    // PUT: api/users/avatar
    [HttpPut("avatar")]
    public async Task<ActionResult<string>> UploadAvatar([FromForm] UploadImageDto uploadImageDto)
    {
        var result = await userServices.UploadAvatarAsync(uploadImageDto.File);
        return ToActionResult(result);
    }

    // GET: api/users/{userId}/avatar
    [HttpGet("{userId}/avatar")]
    public async Task<ActionResult<string>> GetAvatarUrl([FromRoute] string userId)
    {
        var result = await userServices.GetAvatarUrlAsync(userId);
        return ToActionResult(result);
    }

    // DELETE: api/users/avatar
    [HttpDelete("avatar")]
    public async Task<ActionResult<bool>> DeleteAvatar()
    {
        var result = await userServices.DeleteAvatarAsync();
        return ToActionResult(result);
    }

    #endregion

    #region 3. Qu?n lý Bio (Ti?u s?) và C?u hình tài kho?n

    // PUT: api/users/bio
    [HttpPut("bio")]
    public async Task<ActionResult<string>> UpdateBio([FromBody] UpdateBioDto dto) // S?a thành DTO d? tránh l?i null t? [FromBody]
    {
        var result = await userServices.UploadBioAsync(dto.Bio);
        return ToActionResult(result);
    }

    // DELETE: api/users/bio
    [HttpDelete("bio")]
    public async Task<ActionResult<string>> DeleteBio()
    {
        var result = await userServices.DeleteBioAsync();
        return ToActionResult(result);
    }

    // POST/PUT: api/users/privacy/toggle
    // Chuy?n sang HttpPost vì hành d?ng Toggle làm thay d?i tr?ng thái liên t?c (Side-effect), dùng POST chu?n REST hon
    [HttpPost("privacy/toggle")]
    public async Task<ActionResult<string>> ToggleAccountPrivacy()
    {
        var result = await userServices.ToggleAccountPrivacyAsync();
        return ToActionResult(result);
    }

    #endregion

    #region 4. Các Route d?ng (Dynamic Routes) - Ð?t ? cu?i cùng

    // GET: api/users/{targetUserId}
    [HttpGet("{targetUserId}")]
    public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile([FromRoute] string targetUserId)
    {
        var result = await userServices.GetUserProfileAsync(targetUserId);
        return ToActionResult(result);
    }

    #endregion
}

