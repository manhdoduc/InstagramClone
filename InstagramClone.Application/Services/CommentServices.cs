using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using InstagramClone.Application.Interfaces.Caching;

namespace InstagramClone.Application.Services;
public class CommentServices(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache) : ICommentServices
{
    public async Task<Result<ResponseCommentDto>> AddCommentAsync(Guid postId, CreateCommentDto commentDto)
    {
        var userId = currentUser.UserId;
        await cache.BumpScopeVersionAsync($"comments:{postId}:version");

        var postExits = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExits)
            return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.NotFound, "Post not found"));

        var comment = new Comment
        {
            UserId = userId,
            PostId = postId,
            Content = commentDto.Content
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var user = await context.Users.FirstAsync(u => u.Id == userId);

        return Result<ResponseCommentDto>.Success(new ResponseCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            AuthorId = userId,
            AuthorName = user.UserName!,
            AuthorAvatar = user.AvatarUrl
        });
    }

    public async Task<Result<CursorPagedResponse<ResponseCommentDto>>> GetCommentsByPostIdAsync(Guid postId, CursorPaginationRequest pagination)
    {
        var userId = currentUser.UserId;
        var version = await cache.GetScopeVersionAsync($"comments:{postId}:version");

        var cacheKey = $"comments:{postId}:{version}:{userId}:{pagination.Cursor}:{pagination.PageSize}";


        var cachedPost = await cache.GetOrCreateAsync<CursorPagedResponse<ResponseCommentDto>>(cacheKey, factory: async () =>
        {
            var query = context.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId);

            if (pagination.Cursor.HasValue)
                query = query.Where(c => c.CreatedAt < pagination.Cursor.Value);

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(pagination.PageSize + 1)
                .Select(c => new ResponseCommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    AuthorId = c.UserId,
                    AuthorName = c.AppUser.UserName!,
                    AuthorAvatar = c.AppUser.AvatarUrl,
                    LikeCount = c.Likes.Count(),
                    IsLiked = context.CommentLikes.Any(cl => cl.CommentId == c.Id && cl.UserId == userId && !cl.IsDeleted)
                })
                .ToListAsync();

            var hasNextPage = comments.Count > pagination.PageSize;
            DateTime? nextCursor = null;

            if (hasNextPage)
            {
                comments.RemoveAt(pagination.PageSize);
                nextCursor = comments.Last().CreatedAt;
            }

            var cacheEntry = new CursorPagedResponse<ResponseCommentDto>
            {
                Items = comments,
                HasNextPage = hasNextPage,
                NextCursor = nextCursor
            };

            return cacheEntry;
        });

        return Result<CursorPagedResponse<ResponseCommentDto>>.Success(cachedPost);
    }

    public async Task<Result<string>> DeleteCommentAsync(Guid commentId)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "Unauthorized"));

        var comment = await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if(comment is null)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        if(comment.UserId != userId && comment.Post.UserId != userId)
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "You can only delete your own comments or comments on your posts"));

        comment.IsDeleted = true;
        context.Comments.Update(comment);

        try
        {
            var saved = await context.SaveChangesAsync() > 0;
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
        var userId = currentUser.UserId;

        var commentExits = await context.Comments
            .AnyAsync(c => c.Id == commentId);

        if (!commentExits)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        var existingLike = await context.CommentLikes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

        string resultMessage = "";

        if (existingLike == null)
        {
            var like = new CommentLike
            {
                CommentId = commentId,
                UserId = userId
            };
            context.CommentLikes.Add(like);
            resultMessage = LikeCodess.Liked;

        }
        else
        {
            existingLike.IsDeleted = !existingLike.IsDeleted;
            context.CommentLikes.Update(existingLike);
            resultMessage = existingLike.IsDeleted ? LikeCodess.Unlike : LikeCodess.Liked;
        }
        await context.SaveChangesAsync();

        var postId = await context.Comments.AsNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => c.PostId)
            .FirstAsync();
        await cache.BumpScopeVersionAsync($"comments:{postId}:version");

        return Result<string>.Success(resultMessage);

    }
}

