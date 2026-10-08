using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Comments.Mappings;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Comments.Services;

public class CommentServices(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ICacheService cache,
    IBackgroundJobService backgroundJobService
    ) : ICommentServices
{
    public async Task<Result<ResponseCommentDto>> AddCommentAsync(Guid postId, CreateCommentDto commentDto)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var post = await unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null)
            return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.NotFound, "Post not found"));

        var comment = new Comment(postId, userId, commentDto.Content);
        unitOfWork.Comments.Add(comment);
        await unitOfWork.SaveChangesAsync();

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<ResponseCommentDto>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

        // Trigger notification to post author via background job
        backgroundJobService.Enqueue<INotificationServices>(svc =>
            svc.CreateAndSendNotificationAsync(post.UserId, userId, NotificationType.Comment, $"commented on your post: \"{commentDto.Content}\"", postId));

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
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<CursorPagedResponse<ResponseCommentDto>>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var cacheKey = $"comments:{postId}:{pagination.Cursor}:{pagination.PageSize}:{userId}";

        var cachedPost = await cache.GetOrCreateAsync<CursorPagedResponse<ResponseCommentDto>>(
            cacheKey, 
            factory: async () =>
            {
                var comments = await unitOfWork.Comments.GetCommentsByPostIdAsync(postId, pagination.Cursor, pagination.PageSize);
                var dtos = comments.ToResponseCommentDtos(userId);
                return PaginationHelper.ToCursorPaged(dtos, pagination.PageSize, c => c.CreatedAt);
            },
            TimeSpan.FromSeconds(30));

        return Result<CursorPagedResponse<ResponseCommentDto>>.Success(cachedPost);
    }

    public async Task<Result<string>> DeleteCommentAsync(Guid commentId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "Unauthorized"));

        var comment = await unitOfWork.Comments.GetByIdWithPostAsync(commentId);

        if (comment is null)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        if (comment.UserId != userId && comment.Post.UserId != userId && !currentUser.IsAdmin)
            return Result<string>.Failure(new Error(ErrorCodes.Forbid, "You do not have permission to delete this comment"));

        comment.MarkAsDeleted();
        unitOfWork.Comments.Update(comment);

        try
        {
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            return saved
                ? Result<string>.Success("Comment deleted successfully")
                : Result<string>.Failure(new Error(ErrorCodes.Failure, "Failed to delete comment"));
        }
        catch (Exception ex)
        {
            var rootError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Result<string>.Failure(new Error(ErrorCodes.Failure, rootError));
        }
    }

    public async Task<Result<string>> ToggleLikeCommentAsync(Guid commentId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId)) 
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var commentExits = await unitOfWork.Comments.AnyAsync(c => c.Id == commentId);

        if (!commentExits)
            return Result<string>.Failure(new Error(ErrorCodes.NotFound, "Comment not found"));

        var existingLike = await unitOfWork.Comments.GetLikeAsync(userId, commentId, includeDeleted: true);

        string resultMessage = "";

        if (existingLike == null)
        {
            var like = new CommentLike(userId, commentId);
            unitOfWork.Comments.AddLike(like);
            resultMessage = LikeCodess.Liked;
        }
        else
        {
            existingLike.ToggleDeleted();
            unitOfWork.Comments.UpdateLike(existingLike);
            resultMessage = existingLike.IsDeleted ? LikeCodess.Unlike : LikeCodess.Liked;
        }
        await unitOfWork.SaveChangesAsync();

        return Result<string>.Success(resultMessage);
    }
}
