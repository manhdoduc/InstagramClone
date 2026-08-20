using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Posts;

namespace InstagramClone.Web.Services.Comments;

public class CommentService(HttpClient httpClient) : BaseApiService(httpClient), ICommentService
{
    public async Task<ApiResult<CursorPagedResponse<CommentDto>>> GetCommentsAsync(Guid postId, PaginationRequest request)
        => await GetAsync<CursorPagedResponse<CommentDto>>($"/api/posts/{postId}/comments?{request.ToQueryString()}");

    public async Task<ApiResult<CommentDto>> AddCommentAsync(Guid postId, string content)
        => await PostAsync<CommentDto>($"/api/posts/{postId}/comments", new { Content = content });

    public async Task<ApiResult<string>> DeleteCommentAsync(Guid postId, Guid commentId)
        => await DeleteAsync<string>($"/api/posts/{postId}/comments/{commentId}");

    public async Task<ApiResult<string>> ToggleLikeCommentAsync(Guid postId, Guid commentId)
        => await PostAsync<string>($"/api/posts/{postId}/comments/{commentId}/like");
}
