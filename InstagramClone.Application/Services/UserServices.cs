using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using InstagramClone.Application.Interfaces.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Application.Interfaces.Caching;

namespace InstagramClone.Application.Services;
public class UserServices(IStorageServices storageServices, UserManager<AppUser> userManager, 
                            ICurrentUserService currentUser, IApplicationDbContext context,
                            ICacheService cache) : IUserServices
{
    public async Task<Result<string>> UploadAvatarAsync(IFormFile file)
    {
        // 1. Check if the user exists
        string userId = currentUser.UserId; // Override the userId with the current user's ID to ensure users can only upload their own avatars

        var user = await userManager.FindByIdAsync(userId);
        if(user is null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        // 2. Upload file mới
        var uploadResult = await storageServices.UploadImageAsync(file, userId, "avatars", 500, 500);
        if (!uploadResult.IsSuccess)
        {
            return Result<string>.Failure(uploadResult.Errors);
        }

        var avatarUrl = uploadResult.Value!; 

        // Lưu lại link cũ để xóa sau
        var oldAvatarUrl = user.AvatarUrl;

        // 3. Cập nhật link mới vào DB
        user.AvatarUrl = avatarUrl;
        var updateResult = await userManager.UpdateAsync(user);

        // 4. Nếu DB lỗi -> Xóa file MỚI (Rollback)
        if (!updateResult.Succeeded)
        {
            await storageServices.DeleteFile(avatarUrl);
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());
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
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));
        return Result<string>.Success(user.AvatarUrl ?? string.Empty);
    }

    public async Task<Result<bool>> DeleteAvatarAsync()
    {
        var userId = currentUser.UserId;

        if (userId == null)
            return Result<bool>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<bool>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));
        var oldAvatarUrl = user.AvatarUrl;
        if (string.IsNullOrEmpty(oldAvatarUrl))
            return Result<bool>.BadRequest(new Error(ErrorCodes.BadRequest, "No avatar to remove."));
        user.AvatarUrl = AppConstants.DefaultAvatarUrl;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result<bool>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());
        await storageServices.DeleteFile(oldAvatarUrl);
        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");
        return Result<bool>.Success(true);
    }

    public async Task<Result<string>> ToggleAccountPrivacyAsync()
    {
        var userId = currentUser.UserId;

        if (userId == null)
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.IsPrivateAccount = !user.IsPrivateAccount;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

        string privacyStatus = user.IsPrivateAccount ? PrivateAccounts.Private : PrivateAccounts.Public;
        return Result<string>.Success($"Account is now {privacyStatus}.");
    }

    public async Task<Result<string>> UploadBioAsync(string bio)
    {
        var userId = currentUser.UserId;

        if (userId == null)
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));
        
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.Bio = bio;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());

        await cache.BumpScopeVersionAsync($"user:profile:rev:{userId}");

        return Result<string>.Success("Bio updated successfully.");
    }

    public async Task<Result<string>> DeleteBioAsync()
    {
        var userId = currentUser.UserId;
        if (userId == null)
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.Bio = null;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());

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
                var userExists = await context.Users.AnyAsync(u => u.Id == targetUserId);
                if (!userExists) return null;

                bool myAccount = userId == targetUserId;

                var profile = await context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == targetUserId)
                    .Select(u => new UserProfileResponseDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        AvatarUrl = u.AvatarUrl ?? "",
                        Bio = u.Bio,
                        MyAccount = myAccount,
                        IsPrivateAccount = u.IsPrivateAccount,
                        PostCount = context.Posts.Count(p => p.UserId == targetUserId),
                        FollowerCount = u.Followers.Count(f => f.Status == FollowStatus.Accepted),
                        FollowingCount = u.Followings.Count(f => f.Status == FollowStatus.Accepted),
                        // Quan trọng: Phải check ObserverId == userId (người đang xem)
                        IsFollowing = !string.IsNullOrEmpty(userId) && u.Followers.Any(f => f.ObserverId == userId && f.Status == FollowStatus.Accepted),
                        IsRequested = !string.IsNullOrEmpty(userId) && u.Followers.Any(f => f.ObserverId == userId && f.Status == FollowStatus.Pending),
                    })
                    .FirstOrDefaultAsync();

                if (profile == null) return null;

                // 2. LOGIC BẢO MẬT: Load bài viết nếu có quyền
                bool canViewPosts = myAccount || !profile.IsPrivateAccount || profile.IsFollowing;

                if (canViewPosts)
                {
                    profile.RecentPosts = await context.Posts
                        .AsNoTracking()
                        .Where(p => p.UserId == targetUserId)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(12)
                        .Select(p => new PostGridItemDto
                        {
                            Id = p.Id,
                            ThumbnailUrl = p.MediaPorts.OrderBy(m => m.Id).Select(m => m.MediaUrl).FirstOrDefault() ?? "",
                            LikeCount = p.Likes.Count(),
                            CommentCount = p.Comments.Count()
                        })
                        .ToListAsync();
                }

                return profile;
            },
            TimeSpan.FromMinutes(2));

        // 2. KIỂM TRA KẾT QUẢ SAU KHI LẤY TỪ CACHE
        if (cachedProfile == null)
        {
            return Result<UserProfileResponseDto>.Failure(new Error("NotFound", "Người dùng không tồn tại."));
        }

        return Result<UserProfileResponseDto>.Success(cachedProfile);
    }

    public async Task<Result<List<UserSummaryDto>>> SearchUsersAsync(string searchTerm)
    {
        var userId = currentUser.UserId;

        if (string.IsNullOrWhiteSpace(searchTerm))
            return Result<List<UserSummaryDto>>.BadRequest(new Error(ErrorCodes.BadRequest, "Search term cannot be empty."));

        searchTerm = searchTerm.Trim().ToLower();

        var query = context.Users.AsNoTracking().AsQueryable();

        if (searchTerm.StartsWith("@"))
        {
            string usernameSearch = searchTerm.Substring(1);
            query = query.Where(u => u.UserName == usernameSearch);
        }
        else
        {
            query = query.Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(searchTerm));
        }
        var user = await query
            .Take(15)
            .Select(u => new UserSummaryDto
            {
                UserId = u.Id,
                UserName = u.UserName!,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl ?? "",
                IsFollowing = !string.IsNullOrEmpty(userId) && context.Follows.Any(f => f.ObserverId == userId && f.TargetId == u.Id && f.Status == FollowStatus.Accepted)
            })
            .ToListAsync();

        return Result<List<UserSummaryDto>>.Success(user);
    }
}
