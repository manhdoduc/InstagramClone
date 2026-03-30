using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Common.Results;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;
public interface IUserServices
{
    Task<Result<string>> GetAvatarUrlAsync(string userId);
    Task<Result<string>> ToggleAccountPrivacyAsync();
    Task<Result<string>> UploadAvatarAsync(IFormFile file);
    Task<Result<bool>> DeleteAvatarAsync();

    Task<Result<string>> UploadBioAsync(string bio);
    Task<Result<string>> DeleteBioAsync();

    Task<Result<UserProfileResponseDto>> GetUserProfileAsync(string targetUserId);
    Task<Result<List<UserSummaryDto>>> SearchUsersAsync(string searchTerm);
}

