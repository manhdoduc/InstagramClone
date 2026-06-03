using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;

using InstagramClone.Infrastructure.Identity;

namespace InstagramClone.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<AppUser> AppUsers { get; set; }
        // Khai báo DbSet cho các entity sau này
        // public DbSet<Post> Posts { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostMedia> PostMedias { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Follow> Follows { get; set; }

    public DbSet<CommentLike> CommentLikes { get; set; }

    public DbSet<SavedPost> SavedPosts { get; set; }

    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<PostHashtag> PostHashtags { get; set; }

    // chat 
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }


    public DbSet<MessageReaction> MessageReactions { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}


