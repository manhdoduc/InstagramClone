using AutoMapper;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Infrastructure.Persistence;

namespace InstagramClone.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context, IMapper mapper)
    {
        _context = context;
        Users = new UserRepository(_context, mapper);
        Posts = new PostRepository(_context, mapper);
        Comments = new CommentRepository(_context, mapper);
        Chats = new ChatRepository(_context, mapper);
        Notifications = new Persistence.Repositories.NotificationRepository(_context);
    }

    public IUserRepository Users { get; }
    public IPostRepository Posts { get; }
    public ICommentRepository Comments { get; }
    public IChatRepository Chats { get; }
    public INotificationRepository Notifications { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
