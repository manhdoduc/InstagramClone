using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
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

public class PostRepository(AppDbContext context, IMapper mapper) : IPostRepository
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

    public async Task<ResponsePostDto?> GetPostDtoByIdAsync(Guid id, Guid currentUserId)
    {
        return await context.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ResponsePostDto>> GetFeedsAsync(List<Guid> followingIds, DateTime? cursor, int pageSize, Guid currentUserId)
    {
        var query = context.Posts.AsNoTracking()
            .Where(p => followingIds.Contains(p.UserId));

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId })
            .ToListAsync();
    }

    public async Task<List<ResponsePostDto>> GetSavedPostsAsync(Guid userId, DateTime? cursor, int pageSize)
    {
        var query = context.SavedPosts.AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => s.Post);

        if (cursor.HasValue)
        {
            query = query.Where(p => p.CreatedAt < cursor.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId = userId })
            .ToListAsync();
    }

    public async Task<List<ResponsePostDto>> GetSearchPostsAsync(string content, DateTime? cursor, int pageSize, Guid currentUserId)
    {
        content = content.Trim().ToLower();
        var query = context.Posts.AsNoTracking();

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
            .ProjectTo<ResponsePostDto>(mapper.ConfigurationProvider, new { currentUserId })
            .ToListAsync();
    }

    public async Task<List<PostGridItemDto>> GetRecentPostsGridAsync(Guid userId, int limit)
    {
        return await context.Posts.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ProjectTo<PostGridItemDto>(mapper.ConfigurationProvider)
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
