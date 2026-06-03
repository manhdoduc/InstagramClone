using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Application.Interfaces.Caching;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Common;

namespace InstagramClone.Application.Features.Comments.Services;
public class CommentServices(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache, IMapper mapper) : ICommentServices
{
    public async Task<Result<ResponseCommentDto>> AddCommentAsync(Guid postId, CreateCommentDto commentDto)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        await cache.BumpScopeVersionAsync($"comments:{postId}:version");

        var postExits = await unitOfWork.Posts.AnyAsync(p => p.Id == postId);
        if (!postExits)
            return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.NotFound, "Post not found"));

        var comment = new Comment(postId, userId, commentDto.Content);
        unitOfWork.Comments.Add(comment);
        await unitOfWork.SaveChangesAsync();

        var user = await unitOfWork.Users.QueryNoTracking().FirstAsync(u => u.Id == userId);

        return Result<ResponseCommentDto>.Success(new ResponseCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            AuthorId = userId.ToString(),
            AuthorName = user.UserName!,
            AuthorAvatar = user.AvatarUrl
        });
    }

    public async Task<Result<CursorPagedResponse<ResponseCommentDto>>> GetCommentsByPostIdAsync(Guid postId, CursorPaginationRequest pagination)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<CursorPagedResponse<ResponseCommentDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));
        var version = await cache.GetScopeVersionAsync($"comments:{postId}:version");

        var cacheKey = $"comments:{postId}:{version}:{userId}:{pagination.Cursor}:{pagination.PageSize}";


        var cachedPost = await cache.GetOrCreateAsync<CursorPagedResponse<ResponseCommentDto>>(cacheKey, factory: async () =>
        {
            var query = unitOfWork.Comments
            .QueryNoTracking()
            .Where(c => c.PostId == postId);

            if (pagination.Cursor.HasValue)
                query = query.Where(c => c.CreatedAt < pagination.Cursor.Value);

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(pagination.PageSize + 1)
                .ProjectTo<ResponseCommentDto>(mapper.ConfigurationProvider, new { currentUserId = userId })
                .ToListAsync();

            return PaginationHelper.ToCursorPaged(comments, pagination.PageSize, c => c.CreatedAt);
        });

        return Result<CursorPagedResponse<ResponseCommentDto>>.Success(cachedPost);
    }

    public async Task<Result<string>> DeleteCommentAsync(Guid commentId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "Unauthorized"));

        var comment = await unitOfWork.Comments.Query()
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if(comment is null)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        if(comment.UserId != userId && comment.Post.UserId != userId)
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "You can only delete your own comments or comments on your posts"));

        comment.MarkAsDeleted();
        unitOfWork.Comments.Update(comment);

        try
        {
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (saved)
                await cache.BumpScopeVersionAsync($"comments:{comment.PostId}:version");
            return saved
                ? Result<string>.Success("Comment deleted successfully")
                : Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to delete comment"));
        }
        catch (Exception ex)
        {
            var rootError = ex.InnerException != null ? ex.InnerException.Message : ex.Message; // dùng để lấy lỗi gốc nếu có InnerException, nếu không thì lấy lỗi hiện tại
            return Result<string>.Failure(new Error(ErrorCodes.Failure, rootError));
        }
    }

    public async Task<Result<string>> ToggleLikeCommentAsync(Guid commentId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var commentExits = await unitOfWork.Comments
            .AnyAsync(c => c.Id == commentId);

        if (!commentExits)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        var existingLike = await unitOfWork.CommentLikes.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

        string resultMessage = "";

        if (existingLike == null)
        {
            var like = new CommentLike(userId, commentId);
            unitOfWork.CommentLikes.Add(like);
            resultMessage = LikeCodess.Liked;

        }
        else
        {
            existingLike.ToggleDeleted();
            unitOfWork.CommentLikes.Update(existingLike);
            resultMessage = existingLike.IsDeleted ? LikeCodess.Unlike : LikeCodess.Liked;
        }
        await unitOfWork.SaveChangesAsync();

        var postId = await unitOfWork.Comments.QueryNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => c.PostId)
            .FirstAsync();
        await cache.BumpScopeVersionAsync($"comments:{postId}:version");

        return Result<string>.Success(resultMessage);

    }
}

