using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Common.Results;

namespace InstagramClone.Application.Interfaces.Services
{
    public interface ICommentServices
    {
        Task<Result<ResponseCommentDto>> AddCommentAsync(Guid postId, CreateCommentDto commentDto);
        Task<Result<CursorPagedResponse<ResponseCommentDto>>> GetCommentsByPostIdAsync(Guid postId, CursorPaginationRequest pagination);
        Task<Result<string>> DeleteCommentAsync(Guid commentId);

        Task<Result<string>> ToggleLikeCommentAsync(Guid commentId);
    }
}
