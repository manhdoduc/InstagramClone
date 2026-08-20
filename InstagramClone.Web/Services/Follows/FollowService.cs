using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Users;

namespace InstagramClone.Web.Services.Follows;

public class FollowService(HttpClient httpClient) : BaseApiService(httpClient), IFollowService
{
    public async Task<ApiResult<string>> ToggleFollowAsync(string targetUserId)
        => await PostAsync<string>($"/api/users/{targetUserId}/follow");

    public async Task<ApiResult<bool>> AcceptFollowRequestAsync(string followerId)
        => await PutAsync<bool>($"/api/follows/requests/{followerId}/accept");

    public async Task<ApiResult<bool>> DeclineFollowRequestAsync(string followerId)
        => await PutAsync<bool>($"/api/follows/requests/{followerId}/decline");

    public async Task<ApiResult<CursorPagedResponse<UserSummaryDto>>> GetFollowersAsync(string userId, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<UserSummaryDto>>($"/api/users/{userId}/followers?{request.ToQueryString()}");

    public async Task<ApiResult<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string userId, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<UserSummaryDto>>($"/api/users/{userId}/following?{request.ToQueryString()}");
}
