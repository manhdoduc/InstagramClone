using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers
{
    [ApiController]
    [Authorize]
    public class FollowsController(IFollowService followService) : BaseApiController
    {
        #region Thao tác Follow (Yêu cầu / Hủy yêu cầu)

        // POST: api/users/{targetUserId}/follow
        [HttpPost("api/users/{targetUserId}/follow")]
        public async Task<ActionResult<string>> ToggleFollow([FromRoute] string targetUserId)
        {
            var result = await followService.SendFollowRequestAsync(targetUserId);
            return ToActionResult(result);
        }

        #endregion

        #region Quản lý Lời mời theo dõi (Follow Requests)
        // Gom các hành động xử lý lời mời nhận được vào route "requests" công tâm và rõ nghĩa

        // PUT: api/follows/requests/{followerId}/accept
        [HttpPut("api/follows/requests/{followerId}/accept")]
        public async Task<ActionResult<bool>> AcceptFollowRequest([FromRoute] string followerId)
        {
            var result = await followService.AcceptFollowRequestAsync(followerId);
            return ToActionResult(result);
        }

        // PUT: api/follows/requests/{followerId}/decline
        [HttpPut("api/follows/requests/{followerId}/decline")]
        public async Task<ActionResult<bool>> DeclineFollowRequest([FromRoute] string followerId)
        {
            var result = await followService.DeclineFollowRequestAsync(followerId);
            return ToActionResult(result);
        }

        #endregion

        #region Truy vấn danh sách Followers / Following

        // GET: api/users/{userId}/followers
        [HttpGet("api/users/{userId}/followers")]
        public async Task<ActionResult<CursorPagedResponse<UserSummaryDto>>> GetFollowers(
            [FromRoute] string userId,
            [FromQuery] CursorPaginationRequest request)
        {
            var result = await followService.GetFollowerAsync(userId, request);
            return ToActionResult(result);
        }

        // GET: api/users/{userId}/following
        [HttpGet("api/users/{userId}/following")]
        public async Task<ActionResult<CursorPagedResponse<UserSummaryDto>>> GetFollowing(
            [FromRoute] string userId,
            [FromQuery] CursorPaginationRequest request)
        {
            var result = await followService.GetFollowingAsync(userId, request);
            return ToActionResult(result);
        }

        #endregion
    }
}