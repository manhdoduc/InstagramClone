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

public class PostRepository(AppDbContext context) : IPostRepository
{
    public async Task<Post?> GetByIdAsync(Guid id)
    {
        return await context.Posts.FindAsync(id);
    }

    public async Task<Post?> GetByIdWithMediaAsync(Guid id)
    {
        return await context.Posts
            .Include(p => p.MediaItems)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Post post)
    {
        context.Posts.Add(post);
    }

    public void Update(Post post)
    {
        context.Posts.Update(post);
    }

    public async Task<bool> AnyAsync(Expression<Func<Post, bool>> predicate)
    {
        return await context.Posts.AnyAsync(predicate);
    }

    public async Task<Post?> GetPostDetailsByIdAsync(Guid id)
    {
        return await context.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.SavedPosts)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Post>> GetFeedsAsync(List<Guid> followingIds, DateTime? cursor, int pageSize)
    {
        IQueryable<Post> query = context.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.SavedPosts)
            .Where(p => followingIds.Contains(p.UserId));

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<List<Post>> GetSavedPostsAsync(Guid userId, DateTime? cursor, int pageSize)
    {
        IQueryable<Post> query = context.SavedPosts.AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => s.Post)
            .Include(p => p.User)
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.SavedPosts);

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<List<Post>> GetSearchPostsAsync(string content, DateTime? cursor, int pageSize)
    {
        content = content.Trim().ToLower();
        IQueryable<Post> query = context.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.SavedPosts);

        if (content.StartsWith("#"))
        {
            query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name.ToLower() == content));
        }
        else
        {
            query = query.Where(p => EF.Functions.Like(p.Content, $"%{content}%"));
        }

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<List<Post>> GetUserPostsAsync(Guid userId, DateTime? cursor, int pageSize)
    {
        IQueryable<Post> query = context.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.SavedPosts)
            .Where(p => p.UserId == userId);

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<List<Post>> GetRecentPostsGridAsync(Guid userId, int limit)
    {
        return await context.Posts.AsNoTracking()
            .Include(p => p.MediaItems)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Like?> GetLikeAsync(Guid userId, Guid postId, bool includeDeleted = false)
    {
        var query = context.Likes.AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
    }

    public void AddLike(Like like)
    {
        context.Likes.Add(like);
    }

    public void UpdateLike(Like like)
    {
        context.Likes.Update(like);
    }

    public async Task<SavedPost?> GetSavedPostAsync(Guid userId, Guid postId, bool includeDeleted = false)
    {
        var query = context.SavedPosts.AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.FirstOrDefaultAsync(s => s.UserId == userId && s.PostId == postId);
    }

    public void AddSavedPost(SavedPost savedPost)
    {
        context.SavedPosts.Add(savedPost);
    }

    public void UpdateSavedPost(SavedPost savedPost)
    {
        context.SavedPosts.Update(savedPost);
    }

    public async Task<List<Hashtag>> GetHashtagsByNamesAsync(List<string> names)
    {
        return await context.Hashtags.AsNoTracking()
            .Where(h => names.Contains(h.Name))
            .ToListAsync();
    }

    public void AddHashtag(Hashtag hashtag)
    {
        context.Hashtags.Add(hashtag);
    }

    public void AddPostHashtag(PostHashtag postHashtag)
    {
        context.PostHashtags.Add(postHashtag);
    }
}
