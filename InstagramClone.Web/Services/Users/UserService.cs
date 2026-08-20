using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Users;

namespace InstagramClone.Web.Services.Users;

public class UserService(HttpClient httpClient) : BaseApiService(httpClient), IUserService
{
    public async Task<ApiResult<UserProfileDto>> GetUserProfileAsync(string targetUserId)
        => await GetAsync<UserProfileDto>($"/api/users/{targetUserId}");

    public async Task<ApiResult<List<UserSummaryDto>>> SearchUsersAsync(string query)
        => await GetAsync<List<UserSummaryDto>>($"/api/users/search?query={Uri.EscapeDataString(query)}");

    public async Task<ApiResult<string>> UploadAvatarAsync(IBrowserFile file)
    {
        using var form = new MultipartFormDataContent();
        var fileStream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(streamContent, "File", file.Name);

        return await PutFormAsync<string>("/api/users/avatar", form);
    }

    public async Task<ApiResult<bool>> DeleteAvatarAsync()
        => await DeleteAsync<bool>("/api/users/avatar");

    public async Task<ApiResult<string>> UpdateBioAsync(string bio)
        => await PutAsync<string>("/api/users/bio", new { Bio = bio });

    public async Task<ApiResult<string>> DeleteBioAsync()
        => await DeleteAsync<string>("/api/users/bio");

    public async Task<ApiResult<string>> ToggleAccountPrivacyAsync()
        => await PostAsync<string>("/api/users/privacy/toggle");
}
