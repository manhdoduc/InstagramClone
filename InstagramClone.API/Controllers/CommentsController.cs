using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers;

[Route("api/posts/{postId}/[controller]")]
[ApiController]
[Authorize]
public class CommentsController(ICommentServices commentServices) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<ResponseCommentDto>> CreateComment(Guid postId, [FromBody] CreateCommentDto comment)
    {
        var result = await commentServices.AddCommentAsync(postId, comment);
        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResponse<ResponseCommentDto>>> GetComments(Guid postId, [FromQuery] CursorPaginationRequest pagination)
    {
        var result = await commentServices.GetCommentsByPostIdAsync(postId, pagination);
        return ToActionResult(result);
    }

    // Tương tác lên 1 comment cụ thể thuộc post: DELETE api/posts/{postId}/comments/{commentId}
    [HttpDelete("{commentId}")]
    public async Task<ActionResult<string>> DeleteComment(Guid postId, Guid commentId)
    {
        var result = await commentServices.DeleteCommentAsync(commentId);

        return ToActionResult(result);
    }

    // Like/unlike một comment thuộc post: POST api/posts/{postId}/comments/{commentId}/like
    [HttpPost("{commentId}/like")]
    public async Task<ActionResult<string>> ToggleLikeComment(Guid postId, Guid commentId)
    {
        var result = await commentServices.ToggleLikeCommentAsync(commentId);
        return ToActionResult(result);
    }
}

