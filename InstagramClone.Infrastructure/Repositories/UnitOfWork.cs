using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using InstagramClone.Infrastructure.Repositories;

namespace InstagramClone.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Posts = new GenericRepository<Post>(_context);
        Hashtags = new GenericRepository<Hashtag>(_context);
        PostHashtags = new GenericRepository<PostHashtag>(_context);
        SavedPosts = new GenericRepository<SavedPost>(_context);
        Likes = new GenericRepository<Like>(_context);
        Users = new GenericRepository<AppUser>(_context);
        Follows = new GenericRepository<Follow>(_context);
        Comments = new GenericRepository<Comment>(_context);
        CommentLikes = new GenericRepository<CommentLike>(_context);
        ChatRooms = new GenericRepository<ChatRoom>(_context);
        ChatParticipants = new GenericRepository<ChatParticipant>(_context);
        Messages = new GenericRepository<Message>(_context);
        MessageReactions = new GenericRepository<MessageReaction>(_context);
    }

    public IGenericRepository<Post> Posts { get; }
    public IGenericRepository<Hashtag> Hashtags { get; }
    public IGenericRepository<PostHashtag> PostHashtags { get; }
    public IGenericRepository<SavedPost> SavedPosts { get; }
    public IGenericRepository<Like> Likes { get; }
    public IGenericRepository<AppUser> Users { get; }
    public IGenericRepository<Follow> Follows { get; }
    public IGenericRepository<Comment> Comments { get; }
    public IGenericRepository<CommentLike> CommentLikes { get; }
    public IGenericRepository<ChatRoom> ChatRooms { get; }
    public IGenericRepository<ChatParticipant> ChatParticipants { get; }
    public IGenericRepository<Message> Messages { get; }
    public IGenericRepository<MessageReaction> MessageReactions { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
