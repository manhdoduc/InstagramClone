using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using InstagramClone.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Infrastructure.Services;
public class UserServices(IStorageServices storageServices, UserManager<AppUser> userManager, ICurrentUserService currentUser, AppDbContext context) : IUserServices
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

        string privacyStatus = user.IsPrivateAccount ? PrivateAccounts.Private : PrivateAccounts.Public;
        return Result<string>.Success($"Account is now {privacyStatus}.");
    }

    public async Task<Result<string>> UploadBioAsync(string bio)
    {
        var userId = currentUser.UserId;
        if (userId == null)
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));
        
        var user = userManager.FindByIdAsync(userId).Result;
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.Bio = bio;
        var updateResult = userManager.UpdateAsync(user).Result;
        if (!updateResult.Succeeded)
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());

        return Result<string>.Success("Bio updated successfully.");
    }

    public async Task<Result<string>> DeleteBioAsync()
    {
        var userId = currentUser.UserId;
        if (userId == null)
            return Result<string>.BadRequest(new Error(ErrorCodes.BadRequest, "User ID is missing from the current user context."));

        var user = userManager.FindByIdAsync(userId).Result;
        if (user == null)
            return Result<string>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        user.Bio = null;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result<string>.Failure(updateResult.Errors.Select(e => new Error(e.Code, e.Description)).ToArray());

        return Result<string>.Success("Bio deleted successfully.");
    }

    public async Task<Result<UserProfileResponseDto>> GetUserProfileAsync(string targetUserId)
    {
        var userId = currentUser.UserId; // Override the targetUserId with the current user's ID to ensure users can only view their own profile

        var profile = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == targetUserId)
            .Select(u => new UserProfileResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl! ?? "",
                Bio = u.Bio,
                IsPrivateAccount = u.IsPrivateAccount,

                // Đếm số lượng bài viết, followers, following
                PostCount = context.Posts.Count(p => p.UserId == targetUserId),
                FollowerCount = u.Followers.Count(f => f.Status == FollowStatus.Accepted),
                FollowingCount = u.Followings.Count(f => f.Status == FollowStatus.Accepted),

                // Kiểm tra trạng thái follow giữa current user và target user
                IsFollowing = !string.IsNullOrEmpty(userId) && context.Follows.Any( f => f.ObserverId == userId && f.Status == FollowStatus.Accepted),
                IsRequested = !string.IsNullOrEmpty(userId) && context.Follows.Any( f => f.ObserverId == userId && f.Status == FollowStatus.Pending),
            })
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            return Result<UserProfileResponseDto>.Failure(new Error("NotFound", "Người dùng không tồn tại."));
        }
        // 2. LOGIC BẢO MẬT: Có được xem ảnh bài viết không?
        // Được xem khi: Tự xem chính mình HOẶC Tài khoản Public HOẶC Đã follow thành công
        bool canViewPosts = profile.Id == userId || !profile.IsPrivateAccount || profile.IsFollowing;

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
                    // Lấy URL của tấm ảnh đầu tiên trong mảng ảnh của bài viết đó làm Thumbnail
                    ThumbnailUrl = p.MediaPorts.OrderBy(m => m.Id).Select(m => m.MediaUrl).FirstOrDefault() ?? "",
                    LikeCount = p.Likes.Count(),
                    CommentCount = p.Comments.Count()
                })
                .ToListAsync();
        }
        // Nếu không được xem thì RecentPosts mặc định là list rỗng (đã khởi tạo trong DTO)

        return Result<UserProfileResponseDto>.Success(profile);
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
