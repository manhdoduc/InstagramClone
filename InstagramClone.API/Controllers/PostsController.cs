using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController(IPostServices postServices) : BaseApiController
    {
        [HttpPost]
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

        [HttpGet]
        public async Task<ActionResult<List<ResponsePostDto>>> GetFeed([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetFeedAsync(cursorPagination);
            return ToActionResult(result);
        }

        //[HttpGet("list-post")]
        //public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetPosts([FromQuery] CursorPaginationRequest cursorPagination)
        //{
        //    var result = await postServices.GetPostsAsync(cursorPagination);
        //    return ToActionResult(result);
        //}
        
        [HttpPost("{id}/like")] // URL sẽ là: api/posts/{id}/like
        public async Task<ActionResult<string>> ToggleLike(Guid id)
        {
            var result = await postServices.ToggleLikeAsync(id);

            // Nếu thành công, nó sẽ trả về thông báo "Liked" hoặc "Unliked"
            return ToActionResult(result);
        }
        

        [HttpPut("{id}")] // api/posts/{id}
        public async Task<ActionResult<bool>> UpdatePost(Guid id, [FromBody] string content)
        {
            var result = await postServices.UpdatePostAsync(content, id);
            return ToActionResult(result);
        }

        [HttpPost("{id}/toggle-save")] // api/posts/{id}/toggle-save
        public async Task<ActionResult<string>> ToggleSavePost(Guid id)
        {
            var result = await postServices.ToggleSavePostAsync(id);
            return ToActionResult(result);
        }

        [HttpGet("saved-posts")]
        public async Task<ActionResult<List<ResponsePostDto>>> GetSavedPosts([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetSavedPostsAsync(cursorPagination);
            return ToActionResult(result);
        }

        [HttpGet("hashtag")]
        public async Task<ActionResult<List<ResponsePostDto>>> GetPostsByHashtag(
                [FromQuery] string tag,
                [FromQuery] CursorPaginationRequest request)
        {
            var result = await postServices.GetPostsByHashtagAsync(tag, request);
            return ToActionResult(result);
        }
    }
}
