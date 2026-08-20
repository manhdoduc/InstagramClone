using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Users;

namespace InstagramClone.Web.Services.Users;

public interface IUserService
{
    Task<ApiResult<UserProfileDto>> GetUserProfileAsync(string targetUserId);
    Task<ApiResult<List<UserSummaryDto>>> SearchUsersAsync(string query);
    Task<ApiResult<string>> UploadAvatarAsync(IBrowserFile file);
    Task<ApiResult<bool>> DeleteAvatarAsync();
    Task<ApiResult<string>> UpdateBioAsync(string bio);
    Task<ApiResult<string>> DeleteBioAsync();
    Task<ApiResult<string>> ToggleAccountPrivacyAsync();
}
