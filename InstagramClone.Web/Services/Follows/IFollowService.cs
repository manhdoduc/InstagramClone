using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Users;

namespace InstagramClone.Web.Services.Follows;

public interface IFollowService
{
    Task<ApiResult<string>> ToggleFollowAsync(string targetUserId);
    Task<ApiResult<bool>> AcceptFollowRequestAsync(string followerId);
    Task<ApiResult<bool>> DeclineFollowRequestAsync(string followerId);
    Task<ApiResult<CursorPagedResponse<UserSummaryDto>>> GetFollowersAsync(string userId, PaginationRequest request);
    Task<ApiResult<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string userId, PaginationRequest request);
}
