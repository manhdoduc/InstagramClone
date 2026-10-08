using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Repositories;

public class CommentRepository(AppDbContext context) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await context.Comments.FindAsync(id);
    }

    public async Task<Comment?> GetByIdWithPostAsync(Guid id)
    {
        return await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public void Add(Comment comment)
    {
        context.Comments.Add(comment);
    }

    public void Update(Comment comment)
    {
        context.Comments.Update(comment);
    }

    public async Task<bool> AnyAsync(Expression<Func<Comment, bool>> predicate)
    {
        return await context.Comments.AnyAsync(predicate);
    }

    public async Task<Guid> GetPostIdAsync(Guid commentId)
    {
        return await context.Comments.AsNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => c.PostId)
            .FirstAsync();
    }

    public async Task<List<Comment>> GetCommentsByPostIdAsync(Guid postId, DateTime? cursor, int pageSize)
    {
        IQueryable<Comment> query = context.Comments.AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Where(c => c.PostId == postId);

        if (cursor.HasValue)
        {
            query = query.Where(c => c.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<CommentLike?> GetLikeAsync(Guid userId, Guid commentId, bool includeDeleted = false)
    {
        var query = context.CommentLikes.AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.FirstOrDefaultAsync(l => l.UserId == userId && l.CommentId == commentId);
    }

    public void AddLike(CommentLike like)
    {
        context.CommentLikes.Add(like);
    }

    public void UpdateLike(CommentLike like)
    {
        context.CommentLikes.Update(like);
    }
}
