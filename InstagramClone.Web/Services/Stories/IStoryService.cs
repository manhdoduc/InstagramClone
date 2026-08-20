using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Stories;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Web.Services.Stories;

public interface IStoryService
{
    Task<ApiResult<Guid>> CreateStoryAsync(IBrowserFile file, string? caption);
    Task<ApiResult<List<UserStoryFeedDto>>> GetStoriesFeedAsync();
    Task<ApiResult<List<StoryDto>>> GetUserStoriesAsync(Guid targetUserId);
    Task<ApiResult> DeleteStoryAsync(Guid storyId);
}
