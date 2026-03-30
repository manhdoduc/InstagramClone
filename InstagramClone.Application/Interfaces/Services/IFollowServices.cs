using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;

public interface IFollowService
{
    // Người A gửi yêu cầu cho Người B
    Task<Result<string>> SendFollowRequestAsync(string targetId);

    // Người B đồng ý yêu cầu của Người A
    Task<Result<bool>> AcceptFollowRequestAsync(string observerId);

    // Người B từ chối yêu cầu của Người A
    Task<Result<bool>> DeclineFollowRequestAsync(string observerId);

    Task<Result<List<UserSummaryDto>>> GetFollowerAsync(string userId);

    Task<Result<List<UserSummaryDto>>> GetFollowingAsync(string userId);
}
