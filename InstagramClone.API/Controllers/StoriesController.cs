using InstagramClone.Application.Features.Stories.DTOs;
using InstagramClone.Application.Features.Stories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.API.Controllers;

[Route("api/stories")]
[ApiController]
[Authorize]
public class StoriesController(IStoryServices storyServices) : BaseApiController
{
    /// <summary>
    /// Đăng 1 Story mới (ảnh tồn tại trong 24h)
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Guid>> CreateStory([FromForm] CreateStoryDto dto)
    {
        var result = await storyServices.CreateStoryAsync(dto);
        return ToActionResult(result);
    }

    /// <summary>
    /// Lấy danh sách Story 24h của những người đang follow + của chính mình (gom nhóm theo User)
    /// </summary>
    [HttpGet("feed")]
    public async Task<ActionResult<List<UserStoryFeedDto>>> GetStoriesFeed()
    {
        var result = await storyServices.GetStoriesFeedAsync();
        return ToActionResult(result);
    }

    /// <summary>
    /// Lấy danh sách Story 24h của 1 User cụ thể
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<StoryDto>>> GetUserStories([FromRoute] Guid userId)
    {
        var result = await storyServices.GetUserStoriesAsync(userId);
        return ToActionResult(result);
    }

    /// <summary>
    /// Xóa thủ công 1 Story trước 24h
    /// </summary>
    [HttpDelete("{storyId:guid}")]
    public async Task<IActionResult> DeleteStory([FromRoute] Guid storyId)
    {
        var result = await storyServices.DeleteStoryAsync(storyId);
        return ToActionResult(result);
    }
}
