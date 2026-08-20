using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Stories;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace InstagramClone.Web.Services.Stories;

public class StoryService(HttpClient httpClient) : BaseApiService(httpClient), IStoryService
{
    public async Task<ApiResult<Guid>> CreateStoryAsync(IBrowserFile file, string? caption)
    {
        using var form = new MultipartFormDataContent();

        if (!string.IsNullOrWhiteSpace(caption))
        {
            form.Add(new StringContent(caption), "Caption");
        }

        var memoryStream = new MemoryStream();
        using (var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024))
        {
            await fileStream.CopyToAsync(memoryStream);
        }
        memoryStream.Position = 0;

        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(streamContent, "File", file.Name);

        return await PostFormAsync<Guid>("/api/stories", form);
    }

    public async Task<ApiResult<List<UserStoryFeedDto>>> GetStoriesFeedAsync()
        => await GetAsync<List<UserStoryFeedDto>>("/api/stories/feed");

    public async Task<ApiResult<List<StoryDto>>> GetUserStoriesAsync(Guid targetUserId)
        => await GetAsync<List<StoryDto>>($"/api/stories/user/{targetUserId}");

    public async Task<ApiResult> DeleteStoryAsync(Guid storyId)
        => await DeleteAsync($"/api/stories/{storyId}");
}
