using InstagramClone.Web.Models.Common;
using InstagramClone.Web.Models.Posts;

namespace InstagramClone.Web.Services.Comments;

public interface ICommentService
{
    Task<ApiResult<CursorPagedResponse<CommentDto>>> GetCommentsAsync(Guid postId, PaginationRequest request);
    Task<ApiResult<CommentDto>> AddCommentAsync(Guid postId, string content);
    Task<ApiResult<string>> DeleteCommentAsync(Guid postId, Guid commentId);
    Task<ApiResult<string>> ToggleLikeCommentAsync(Guid postId, Guid commentId);
}
