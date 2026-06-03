using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
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
        #region 1. Các Route Tĩnh (Static Routes) - Nên đặt lên trước để tránh trùng lặp

        // GET: api/posts/feed
        [HttpGet("feed")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetPosts([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetFeedsAsync(cursorPagination);
            return ToActionResult(result);
        }

        // GET: api/posts/saved-posts
        [HttpGet("saved-posts")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetSavedPosts([FromQuery] CursorPaginationRequest cursorPagination)
        {
            var result = await postServices.GetSavedPostsAsync(cursorPagination);
            return ToActionResult(result);
        }

        // GET: api/posts/search?content=abc
        [HttpGet("search")]
        public async Task<ActionResult<CursorPagedResponse<ResponsePostDto>>> GetPostsByHashtag(
                [FromQuery] string content,
                [FromQuery] CursorPaginationRequest request)
        {
            var result = await postServices.GetSearchPostsAsync(content, request);
            return ToActionResult(result);
        }

        #endregion

        #region 2. Các Thao tác CRUD Cơ bản (Dùng ràng buộc :guid)

        // POST: api/posts
        [HttpPost]
        [EnableRateLimiting("UploadLimit")]
        public async Task<ActionResult<string>> CreatePost([FromForm] CreatePostDto createPostDto)
        {
            var result = await postServices.CreatePostAsync(createPostDto);
            return ToActionResult(result);
        }

        // GET: api/posts/{id}
        [HttpGet("{id:guid}")] // Thêm :guid để tránh xung đột với feed, search, saved-posts
        public async Task<ActionResult<ResponsePostDto>> GetPostById([FromRoute] Guid id)
        {
            var result = await postServices.GetPostByIdAsync(id);
            return ToActionResult(result);
        }

        // PUT: api/posts/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<bool>> UpdatePost([FromRoute] Guid id, [FromBody] UpdatePostDto dto)
        {
            var result = await postServices.UpdatePostAsync(dto.Content, id);
            return ToActionResult(result);
        }

        // DELETE: api/posts/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeletePost([FromRoute] Guid id)
        {
            var result = await postServices.DeletePostAsync(id);
            return ToActionResult(result);
        }

        #endregion

        #region 3. Các Tương tác / Hành động phụ trên Bài viết (Actions)

        // POST/PUT: api/posts/{id}/like
        // Lưu ý nhỏ: Hành động Like/Unlike mang tính chất "thay đổi trạng thái" (side-effect), 
        // bạn dùng PUT là tạm ổn, nhưng chuẩn REST thường thích dùng POST hơn cho các dạng "Toggle" action.
        [HttpPost("{id:guid}/like")]
        public async Task<ActionResult<string>> ToggleLike([FromRoute] Guid id)
        {
            var result = await postServices.ToggleLikeAsync(id);
            return ToActionResult(result);
        }

        // POST/PUT: api/posts/{id}/toggle-save
        [HttpPost("{id:guid}/toggle-save")]
        public async Task<ActionResult<string>> ToggleSavePost([FromRoute] Guid id)
        {
            var result = await postServices.ToggleSavePostAsync(id);
            return ToActionResult(result);
        }

        #endregion
    }
}