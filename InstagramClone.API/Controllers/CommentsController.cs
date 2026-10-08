using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InstagramClone.API.Controllers;

[Route("api/posts/{postId:guid}/comments")]
[ApiController]
[Authorize]
public class CommentsController(ICommentServices commentServices) : BaseApiController
{
    // POST: api/posts/{postId}/comments
    [HttpPost]
    [EnableRateLimiting("CommentLimit")]
    public async Task<ActionResult<ResponseCommentDto>> CreateComment(
        [FromRoute] Guid postId,
        [FromBody] CreateCommentDto comment)
    {
        var result = await commentServices.AddCommentAsync(postId, comment);
        return ToActionResult(result);
    }

    // GET: api/posts/{postId}/comments?cursor=...&pageSize=...
    [HttpGet]
    public async Task<ActionResult<CursorPagedResponse<ResponseCommentDto>>> GetComments(
        [FromRoute] Guid postId,
        [FromQuery] CursorPaginationRequest pagination)
    {
        var result = await commentServices.GetCommentsByPostIdAsync(postId, pagination);
        return ToActionResult(result);
    }

    // DELETE: api/posts/{postId}/comments/{commentId}
    [HttpDelete("{commentId:guid}")]
    public async Task<ActionResult<string>> DeleteComment(
        [FromRoute] Guid postId,
        [FromRoute] Guid commentId)
    {
        var result = await commentServices.DeleteCommentAsync(commentId);
        return ToActionResult(result);
    }

    // POST: api/posts/{postId}/comments/{commentId}/like
    [HttpPost("{commentId:guid}/like")]
    public async Task<ActionResult<string>> ToggleLikeComment(
        [FromRoute] Guid postId,
        [FromRoute] Guid commentId)
    {
        var result = await commentServices.ToggleLikeCommentAsync(commentId);
        return ToActionResult(result);
    }
}