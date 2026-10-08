using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Infrastructure.Persistence;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Posts = new PostRepository(_context);
        Comments = new CommentRepository(_context);
        Chats = new ChatRepository(_context);
        Notifications = new Persistence.Repositories.NotificationRepository(_context);
        Stories = new StoryRepository(_context);
    }

    public IUserRepository Users { get; }
    public IPostRepository Posts { get; }
    public ICommentRepository Comments { get; }
    public IChatRepository Chats { get; }
    public INotificationRepository Notifications { get; }
    public IStoryRepository Stories { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
