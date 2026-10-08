using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Features.Posts.Mappings;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Features.Users.Mappings;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Helper;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InstagramClone.Infrastructure.Services;
public class UserServices(IStorageServices storageServices, 
                            ICurrentUserService currentUser, IUnitOfWork unitOfWork,
                            ICacheService cache, 
                            IBackgroundJobService backgroundJobService,
                            IOptions<MediaSettings> mediaSettingsOptions) : IUserServices
{
    private readonly MediaSettings _mediaSettings = mediaSettingsOptions.Value;

    public async Task<Result<string>> UploadAvatarAsync(IFormFile file)
    {
        // 1. Check if the user exists
        string userIdStr = currentUser.UserId; // Override the userId with the current user's ID to ensure users can only upload their own avatars
        if (!Guid.TryParse(userIdStr, out var userId))
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if(user is null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        // 2. Upload file mới
        var uploadResult = await storageServices.UploadImageAsync(file, userId.ToString(), "avatars", _mediaSettings.Avatar.MaxWidth, _mediaSettings.Avatar.MaxHeight);
        if (!uploadResult.IsSuccess)
        {
            return Result<string>.Failure(uploadResult.Errors);
        }

        var avatarUrl = uploadResult.Value!; 

        // Lưu lại link cũ để xóa sau
        var oldAvatarUrl = user.AvatarUrl;

        // 3. Cập nhật link mới vào DB
        user.UpdateAvatarUrl(avatarUrl);
        unitOfWork.Users.Update(user);
        var updateResult = await unitOfWork.SaveChangesAsync();

        // 4. Nếu DB lỗi -> Xóa file MỚI (Rollback)
        if (updateResult <= 0)
        {
            await storageServices.DeleteFile(avatarUrl);
            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to update user avatar in database."));
        }

        // 5. Nếu DB THÀNH CÔNG -> Lúc này mới xóa file CŨ (Dọn rác qua Delayed Job)
        if (!string.IsNullOrEmpty(oldAvatarUrl) && oldAvatarUrl != AppConstants.DefaultAvatarUrl)
        {
            backgroundJobService.Schedule<IStorageServices>(
                svc => svc.DeleteFile(oldAvatarUrl!),
                TimeSpan.FromSeconds(10));
        }

        await cache.RemoveAsync($"user:profile:{userId}:{userId}");

        // Trả về link ảnh mới để Frontend hiển thị luôn, đừng trả về text "Success"
        return Result<string>.Success(avatarUrl);
    }

    public async Task<Result<string>> GetAvatarUrlAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var user = await unitOfWork.Users.GetByIdAsync(parsedUserId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));
        return Result<string>.Success(user.AvatarUrl ?? string.Empty);
    }

    public async Task<Result<bool>> DeleteAvatarAsync()
    {
        var userIdStr = currentUser.UserId;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Result<bool>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing or invalid."));
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));
        var oldAvatarUrl = user.AvatarUrl;
        if (string.IsNullOrEmpty(oldAvatarUrl) || oldAvatarUrl == AppConstants.DefaultAvatarUrl)
            return Result<bool>.BadRequest(new Error(ErrorCodes.BadRequest, "No avatar to remove."));
        user.UpdateAvatarUrl(AppConstants.DefaultAvatarUrl);
        
        unitOfWork.Users.Update(user);
        var updateResult = await unitOfWork.SaveChangesAsync();
        if (updateResult <= 0)
            return Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to delete user avatar in database."));
            
        // Xóa file cũ qua Delayed Job
        backgroundJobService.Schedule<IStorageServices>(
            svc => svc.DeleteFile(oldAvatarUrl!),
            TimeSpan.FromSeconds(10));
            
        await cache.RemoveAsync($"user:profile:{userId}:{userId}");
        return Result<bool>.Success(true);
    }

    public async Task<Result<string>> ToggleAccountPrivacyAsync()
    {
        var userIdStr = currentUser.UserId;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing or invalid."));

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.SetAccountPrivacy(!user.IsPrivateAccount);
        
        unitOfWork.Users.Update(user);
        var updateResult = await unitOfWork.SaveChangesAsync();
        if (updateResult <= 0)
            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to update account privacy."));

        await cache.RemoveAsync($"user:profile:{userId}:{userId}");

        string privacyStatus = user.IsPrivateAccount ? PrivateAccounts.Private : PrivateAccounts.Public;
        return Result<string>.Success($"Account is now {privacyStatus}.");
    }

    public async Task<Result<string>> UploadBioAsync(string bio)
    {
        var userIdStr = currentUser.UserId;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing or invalid."));
        
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.UpdateBio(bio);
        unitOfWork.Users.Update(user);
        var updateResult = await unitOfWork.SaveChangesAsync();
        if (updateResult <= 0)
            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to update bio."));

        await cache.RemoveAsync($"user:profile:{userId}:{userId}");

        return Result<string>.Success("Bio updated successfully.");
    }

    public async Task<Result<string>> DeleteBioAsync()
    {
        var userIdStr = currentUser.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing or invalid."));

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.UpdateBio(null);
        unitOfWork.Users.Update(user);
        var updateResult = await unitOfWork.SaveChangesAsync();
        if (updateResult <= 0)
            return Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to delete bio."));

        await cache.RemoveAsync($"user:profile:{userId}:{userId}");

        return Result<string>.Success("Bio deleted successfully.");
    }

    public async Task<Result<UserProfileResponseDto>> GetUserProfileAsync(string targetUserId)
    {
        var userId = currentUser.UserId;
        var cacheKey = $"user:profile:{targetUserId}:{userId}";

        var cachedProfile = await cache.GetOrCreateAsync<UserProfileResponseDto?>(
            cacheKey,
            factory: async () =>
            {
                if (!Guid.TryParse(targetUserId, out var targetUserIdGuid)) return null;

                var targetUser = await unitOfWork.Users.GetUserProfileDetailsAsync(targetUserIdGuid);
                if (targetUser == null) return null;

                _ = Guid.TryParse(userId, out var currentUserIdGuid);
                var profile = targetUser.ToUserProfileResponseDto(currentUserIdGuid);

                bool myAccount = !string.IsNullOrEmpty(userId) && userId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase);
                bool canViewPosts = myAccount || !profile.IsPrivateAccount || profile.IsFollowing;

                if (canViewPosts)
                {
                    var recentPosts = await unitOfWork.Posts.GetRecentPostsGridAsync(targetUserIdGuid, 12);
                    profile.RecentPosts = recentPosts.ToPostGridItemDtos();
                }

                return profile;
            },
            TimeSpan.FromMinutes(2));

        if (cachedProfile == null)
        {
            return Result<UserProfileResponseDto>.Failure(new Error("NotFound", "User does not exist."));
        }

        return Result<UserProfileResponseDto>.Success(cachedProfile);
    }

    public async Task<Result<List<UserSummaryDto>>> SearchUsersAsync(string searchTerm)
    {
        var userId = currentUser.UserId;

        if (string.IsNullOrWhiteSpace(searchTerm))
            return Result<List<UserSummaryDto>>.BadRequest(new Error(ErrorCodes.BadRequest, "Search term cannot be empty."));

        if (!Guid.TryParse(userId, out var currentUserId))
            return Result<List<UserSummaryDto>>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID."));

        var users = await unitOfWork.Users.SearchUsersAsync(searchTerm);
        var userDtos = users.ToUserSummaryDtos(currentUserId);

        return Result<List<UserSummaryDto>>.Success(userDtos);
    }
}
