using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Data;

public interface IApplicationDbContext
{
    DbSet<AppUser> AppUsers { get; set; }
    DbSet<Post> Posts { get; set; }
    DbSet<PostMedia> PostMedias { get; set; }
    DbSet<Comment> Comments { get; set; }
    DbSet<Like> Likes { get; set; }
    DbSet<Follow> Follows { get; set; }
    DbSet<CommentLike> CommentLikes { get; set; }
    DbSet<SavedPost> SavedPosts { get; set; }
    DbSet<Hashtag> Hashtags { get; set; }
    DbSet<PostHashtag> PostHashtags { get; set; }
    DbSet<ChatRoom> ChatRooms { get; set; }
    DbSet<ChatParticipant> ChatParticipants { get; set; }
    DbSet<Message> Messages { get; set; }
    DbSet<MessageReaction> MessageReactions { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
