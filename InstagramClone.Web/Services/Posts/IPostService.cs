using Microsoft.AspNetCore.Components.Forms;
using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Posts;

namespace InstagramClone.Web.Services.Posts;

public interface IPostService
{
    Task<ApiResult<CursorPagedResponse<PostDto>>> GetFeedAsync(PaginationRequest request);
    Task<ApiResult<PostDto>> GetPostByIdAsync(Guid id);
    Task<ApiResult<CursorPagedResponse<PostDto>>> GetUserPostsAsync(string userId, PaginationRequest request);
    Task<ApiResult<CursorPagedResponse<PostDto>>> GetSavedPostsAsync(PaginationRequest request);
    Task<ApiResult<CursorPagedResponse<PostDto>>> SearchPostsAsync(string content, PaginationRequest request);
    Task<ApiResult<string>> CreatePostAsync(string? content, List<IBrowserFile> files);
    Task<ApiResult<bool>> UpdatePostAsync(Guid id, string content);
    Task<ApiResult> DeletePostAsync(Guid id);
    Task<ApiResult<string>> ToggleLikeAsync(Guid id);
    Task<ApiResult<string>> ToggleSaveAsync(Guid id);
}
