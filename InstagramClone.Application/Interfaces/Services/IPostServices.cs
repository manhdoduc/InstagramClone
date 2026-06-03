using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services
{
    public interface IPostServices
    {
        Task<Result<string>> CreatePostAsync(CreatePostDto createPostDto);
        Task<Result> DeletePostAsync(Guid postId);

        Task<Result<ResponsePostDto>> GetPostByIdAsync(Guid id);
        Task<Result<CursorPagedResponse<ResponsePostDto>>> GetFeedsAsync(CursorPaginationRequest cursorPagination);

        Task<Result<bool>> UpdatePostAsync(string content, Guid id);
        //Task<Result<List<ResponsePostDto>>> GetFeedAsync(CursorPaginationRequest cursorPagination);

        Task<Result> ToggleSavePostAsync(Guid postId);

        Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSavedPostsAsync(CursorPaginationRequest cursorPagination);
        Task<Result<CursorPagedResponse<ResponsePostDto>>> GetSearchPostsAsync(string content, CursorPaginationRequest request);
        Task<Result> ToggleLikeAsync(Guid postId);
    }
}
