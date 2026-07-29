using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Common.Results;

namespace InstagramClone.Application.Interfaces.Services;

public interface IFollowService
{
    // Người A gửi yêu cầu cho Người B
    Task<Result<string>> SendFollowRequestAsync(string followeeId);

    // Người B đồng ý yêu cầu của Người A
    Task<Result<bool>> AcceptFollowRequestAsync(string followerId);

    // Người B từ chối yêu cầu của Người A
    Task<Result<bool>> DeclineFollowRequestAsync(string followerId);

    Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowerAsync(string userId, CursorPaginationRequest request);

    Task<Result<CursorPagedResponse<UserSummaryDto>>> GetFollowingAsync(string userId, CursorPaginationRequest request);
}
