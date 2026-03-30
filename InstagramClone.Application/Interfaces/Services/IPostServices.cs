using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
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
        Task<Result<CursorPagedResponse<ResponsePostDto>>> GetPostsAsync(CursorPaginationRequest cursorPagination);

        Task<Result<bool>> UpdatePostAsync(string content, Guid id);
        Task<Result<List<ResponsePostDto>>> GetFeedAsync(CursorPaginationRequest cursorPagination);

        Task<Result<string>> ToggleSavePostAsync(Guid postId);

        Task<Result<List<ResponsePostDto>>> GetSavedPostsAsync(CursorPaginationRequest cursorPagination);
        Task<Result<List<ResponsePostDto>>> GetPostsByHashtagAsync(string tag, CursorPaginationRequest request);
        Task<Result<string>> ToggleLikeAsync(Guid postId);
    }
}
