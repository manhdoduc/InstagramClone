using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;

namespace InstagramClone.Application.Interfaces.Data;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IChatRepository Chats { get; }
    INotificationRepository Notifications { get; }
    
    Task<int> SaveChangesAsync();
}
