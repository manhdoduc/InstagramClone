using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Helper;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Infrastructure.Identity;

namespace InstagramClone.Infrastructure.Services;
public class UserServices(IStorageServices storageServices, 
                            ICurrentUserService currentUser, IUnitOfWork unitOfWork,
                            ICacheService cache, IMapper mapper) : IUserServices
{
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
        var uploadResult = await storageServices.UploadImageAsync(file, userId.ToString(), "avatars", 500, 500);
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

        // 5. Nếu DB THÀNH CÔNG -> Lúc này mới xóa file CŨ (Dọn rác)
        if (!string.IsNullOrEmpty(oldAvatarUrl))
        {
            // Thêm ! để báo cho VS biết oldAvatarUrl chắc chắn không null ở đây
            await storageServices.DeleteFile(oldAvatarUrl!);
        }

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

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
        await storageServices.DeleteFile(oldAvatarUrl);
        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
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

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

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

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

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

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

        return Result<string>.Success("Bio deleted successfully.");
    }

    /// <summary>
    /// Cache: scope <c>user:profile:rev:{targetUserId}</c> (token bump khi profile target đổi — xem PostServices / UserServices).
    /// Entry: <c>user:profile:{viewerId}:{targetUserId}:{rev}</c> — mỗi cặp người xem / người được xem một bản; rev đổi thì key mới, entry cũ hết hiệu lực dần (TTL).
    /// </summary>
    public async Task<Result<UserProfileResponseDto>> GetUserProfileAsync(string targetUserId)
    {
        var userId = currentUser.UserId;
        var profileRev = await cache.GetScopeVersionAsync($"user:profile:rev:{targetUserId}");
        var cacheKey = $"user:profile:{userId}:{targetUserId}:{profileRev}";

        // 1. Gọi Cache (Lưu ý: Factory chỉ trả về DTO hoặc null)
        var cachedProfile = await cache.GetOrCreateAsync<UserProfileResponseDto?>(
            cacheKey,
            factory: async () =>
            {
                // TÌM USER TRƯỚC
                if (!Guid.TryParse(targetUserId, out var targetUserIdGuid)) return null;
                var userExists = await unitOfWork.Users.AnyAsync(u => u.Id == targetUserIdGuid);
                if (!userExists) return null;

                bool myAccount = userId == targetUserId;

                var profile = await unitOfWork.Users.QueryNoTracking()
                    .Where(u => u.Id == targetUserIdGuid)
                    .ProjectTo<UserProfileResponseDto>(mapper.ConfigurationProvider, new { currentUserId = userId, targetUserId = targetUserId })
                    .FirstOrDefaultAsync();

                if (profile == null) return null;

                // 2. LOGIC BẢO MẬT: Load bài viết nếu có quyền
                bool canViewPosts = myAccount || !profile.IsPrivateAccount || profile.IsFollowing;

                if (canViewPosts)
                {
                    profile.RecentPosts = await unitOfWork.Posts.QueryNoTracking()
                        .Where(p => p.UserId == targetUserIdGuid)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(12)
                        .ProjectTo<PostGridItemDto>(mapper.ConfigurationProvider)
                        .ToListAsync();
                }

                return profile;
            },
            TimeSpan.FromMinutes(2));

        // 2. KIỂM TRA KẾT QUẢ SAU KHI LẤY TỪ CACHE
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

        searchTerm = RemoveDiacritics.RemoveDiacritic(searchTerm.Trim());

        var query = unitOfWork.Users.QueryNoTracking();

        if (searchTerm.StartsWith("@"))
        {
            string usernameSearch = searchTerm.Substring(1);
            query = query.Where(u => u.UserName == usernameSearch);
        }
        else
        {
            query = query.Where(u => (u.FullNameSearch!).Contains(searchTerm));
        }
        var user = await query
            .Take(15)
            .ProjectTo<UserSummaryDto>(mapper.ConfigurationProvider, new { currentUserId = userId })
            .ToListAsync();

        return Result<List<UserSummaryDto>>.Success(user);
    }
}
