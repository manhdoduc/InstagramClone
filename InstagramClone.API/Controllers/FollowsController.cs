using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers
{
    [Route("api/follows")]
    [ApiController]
    [Authorize]
    public class FollowsController(IFollowService followService) : BaseApiController
    {
        // Endpoint: POST api/follows/{targetId}
        [HttpPost("{targetId}")]
        public async Task<ActionResult<string>> ToggleFollow(string targetId)
        {
            var result = await followService.SendFollowRequestAsync(targetId);

            // Tận dụng BaseApiController để map Result ra HTTP Status Code chuẩn xác
            return ToActionResult(result);
        }

        // api/follows/{observerId}/accept
        [HttpPut("{observerId}/accept")]
        public async Task<ActionResult<bool>> AcceptFollowRequest(string observerId)
        {
            var result = await followService.AcceptFollowRequestAsync(observerId);
            return ToActionResult(result);
        }

        // api/follows/{observerId}/decline
        [HttpDelete("{observerId}/decline")]
        public async Task<ActionResult<bool>> DeclineFollowRequest(string observerId)
        {
            var result = await followService.DeclineFollowRequestAsync(observerId);
            return ToActionResult(result);
        }

        //
        [HttpGet("{targetId}/followers")]
        public async Task<ActionResult<List<UserSummaryDto>>> GetFollowers(string targetId)
        {
            var result = await followService.GetFollowerAsync(targetId);
            return ToActionResult(result);
        }

        [HttpGet("{targetId}/following")]
        public async Task<ActionResult<List<UserSummaryDto>>> GetFollowing(string targetId)
        {
            var result = await followService.GetFollowingAsync(targetId);
            return ToActionResult(result);
        }
    }
}
