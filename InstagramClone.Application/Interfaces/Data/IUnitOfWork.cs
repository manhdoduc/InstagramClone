using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Interfaces.Data;

public interface IUnitOfWork
{
    IGenericRepository<Post> Posts { get; }
    IGenericRepository<Hashtag> Hashtags { get; }
    IGenericRepository<PostHashtag> PostHashtags { get; }
    IGenericRepository<SavedPost> SavedPosts { get; }
    IGenericRepository<Like> Likes { get; }
    IGenericRepository<AppUser> Users { get; }
    IGenericRepository<Follow> Follows { get; }
    IGenericRepository<Comment> Comments { get; }
    IGenericRepository<CommentLike> CommentLikes { get; }
    IGenericRepository<ChatRoom> ChatRooms { get; }
    IGenericRepository<ChatParticipant> ChatParticipants { get; }
    IGenericRepository<Message> Messages { get; }
    IGenericRepository<MessageReaction> MessageReactions { get; }
    
    Task<int> SaveChangesAsync();
}
