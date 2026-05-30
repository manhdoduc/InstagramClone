using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InstagramClone.API.Controllers
{
    [Route("api/posts")]
    [ApiController]
    [Authorize]
    public class PostsController(IPostServices postServices) : BaseApiController
    {
        [HttpPost]
        [EnableRateLimiting("UploadLimit")]
        public async Task<ActionResult<string>> CreatePost([FromForm] CreatePostDto createPostDto)
        {
            var result = await postServices.CreatePostAsync(createPostDto);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")] // api/posts/{id}
        public async Task<ActionResult> DeletePost(Guid id)
        {
            var result = await postServices.DeletePostAsync(id);

            // Trả về NoContent (204) nếu thành công hoặc lỗi từ BaseApiController
            return ToActionResult(result);
        }

        [HttpGet("{id}")] // api/posts/{id}
        public async Task<ActionResult<ResponsePostDto>> GetPostById(Guid id)
        {
            var result = await postServices.GetPostByIdAsync(id);
            return ToActionResult(result);
        }



        [HttpGet("feed")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetPosts([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetFeedsAsync(cursorPagination);
            return ToActionResult(result);
        }

        [HttpPost("{id}/like")] // URL sẽ là: api/posts/{id}/like
        public async Task<ActionResult<string>> ToggleLike(Guid id)
        {
            var result = await postServices.ToggleLikeAsync(id);

            // Nếu thành công, nó sẽ trả về thông báo "Liked" hoặc "Unliked"
            return ToActionResult(result);
        }
        

        [HttpPut("{id}")] // api/posts/{id}
        public async Task<ActionResult<bool>> UpdatePost(Guid id, [FromBody] UpdatePostDto dto)
        {
            var result = await postServices.UpdatePostAsync(dto.Content, id);
            return ToActionResult(result);
        }

        [HttpPost("{id}/toggle-save")] // api/posts/{id}/toggle-save
        public async Task<ActionResult<string>> ToggleSavePost(Guid id)
        {
            var result = await postServices.ToggleSavePostAsync(id);
            return ToActionResult(result);
        }

        [HttpGet("saved-posts")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetSavedPosts([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetSavedPostsAsync(cursorPagination);
            return ToActionResult(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetPostsByHashtag(
                [FromQuery] string content,
                [FromQuery] CursorPaginationRequest request)
        {
            var result = await postServices.GetSearchPostsAsync(content, request);
            return ToActionResult(result);
        }
    }
}
