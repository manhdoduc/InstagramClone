using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id);
    Task<Comment?> GetByIdWithPostAsync(Guid id);
    void Add(Comment comment);
    void Update(Comment comment);
    Task<bool> AnyAsync(Expression<Func<Comment, bool>> predicate);
    Task<Guid> GetPostIdAsync(Guid commentId);

    Task<List<ResponseCommentDto>> GetCommentsByPostIdAsync(Guid postId, DateTime? cursor, int pageSize, Guid currentUserId);

    // Comment Likes
    Task<CommentLike?> GetLikeAsync(Guid userId, Guid commentId, bool includeDeleted = false);
    void AddLike(CommentLike like);
    void UpdateLike(CommentLike like);
}
