using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface IPostRepository
{
    // Basic CRUD
    Task<Post?> GetByIdAsync(Guid id);
    Task<Post?> GetByIdWithMediaAsync(Guid id);
    void Add(Post post);
    void Update(Post post);
    Task<bool> AnyAsync(Expression<Func<Post, bool>> predicate);

    // Queries (Returns Domain Entities)
    Task<Post?> GetPostDetailsByIdAsync(Guid id);
    Task<List<Post>> GetFeedsAsync(List<Guid> followingIds, DateTime? cursor, int pageSize);
    Task<List<Post>> GetSavedPostsAsync(Guid userId, DateTime? cursor, int pageSize);
    Task<List<Post>> GetSearchPostsAsync(string content, DateTime? cursor, int pageSize);
    Task<List<Post>> GetUserPostsAsync(Guid userId, DateTime? cursor, int pageSize);
    Task<List<Post>> GetRecentPostsGridAsync(Guid userId, int limit);

    // Likes & Saves (Sub-entities/Relationships)
    Task<Like?> GetLikeAsync(Guid userId, Guid postId, bool includeDeleted = false);
    void AddLike(Like like);
    void UpdateLike(Like like);

    Task<SavedPost?> GetSavedPostAsync(Guid userId, Guid postId, bool includeDeleted = false);
    void AddSavedPost(SavedPost savedPost);
    void UpdateSavedPost(SavedPost savedPost);
    
    // Hashtags
    Task<List<Hashtag>> GetHashtagsByNamesAsync(List<string> names);
    void AddHashtag(Hashtag hashtag);
    void AddPostHashtag(PostHashtag postHashtag);
}
