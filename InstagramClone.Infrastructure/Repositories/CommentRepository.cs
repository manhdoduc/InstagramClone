using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InstagramClone.Infrastructure.Repositories;

public class CommentRepository(AppDbContext context, IMapper mapper) : ICommentRepository
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

    public async Task<List<ResponseCommentDto>> GetCommentsByPostIdAsync(Guid postId, DateTime? cursor, int pageSize, Guid currentUserId)
    {
        var query = context.Comments.AsNoTracking()
            .Where(c => c.PostId == postId);

        if (cursor.HasValue)
        {
            query = query.Where(c => c.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(pageSize + 1)
            .ProjectTo<ResponseCommentDto>(mapper.ConfigurationProvider, new { currentUserId })
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
