using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InstagramClone.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
namespace InstagramClone.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
    }
}

public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("Roles");
    }
}

public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.ToTable("UserRoles");
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<AppUser>(u => u.Id);

        builder.HasQueryFilter(u => !u.IsDeleted);
        builder.HasIndex(u => u.FullNameSearch);
        builder.HasIndex(u => u.UserName);
        builder.HasIndex(u => u.Email);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.UserId, p.CreatedAt });

        builder.HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.HasOne(m => m.Post)
            .WithMany(p => p.MediaItems)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Composite index: load comments of a post sorted by time (GetCommentsByPostIdAsync)
        builder.HasIndex(c => new { c.PostId, c.CreatedAt })
            .HasDatabaseName("IX_Comments_PostId_CreatedAt");

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();

        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasQueryFilter(f => !f.IsDeleted);

        // Unique index: mỗi cặp (Follower, Followee) chỉ tồn tại một lần → phòng chống race condition
        builder.HasIndex(f => new { f.FollowerId, f.FolloweeId })
            .IsUnique()
            .HasDatabaseName("IX_Follows_FollowerId_FolloweeId_Unique");

        // Covering index cho GetFollowersAsync: WHERE FolloweeId = X AND Status = Accepted ORDER BY CreatedAt DESC
        builder.HasIndex(f => new { f.FolloweeId, f.Status, f.CreatedAt })
            .HasDatabaseName("IX_Follows_FolloweeId_Status_CreatedAt");

        // Covering index cho GetFollowingAsync: WHERE FollowerId = X AND Status = Accepted ORDER BY CreatedAt DESC
        builder.HasIndex(f => new { f.FollowerId, f.Status, f.CreatedAt })
            .HasDatabaseName("IX_Follows_FollowerId_Status_CreatedAt");

        builder.HasOne(f => f.Follower)
            .WithMany(u => u.Followings)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Followee)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.HasQueryFilter(cl => !cl.IsDeleted);

        builder.HasIndex(cl => new { cl.CommentId, cl.UserId }).IsUnique();

        builder.HasOne(cl => cl.Comment)
            .WithMany(c => c.Likes)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cl => cl.User)
            .WithMany()
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
{
    public void Configure(EntityTypeBuilder<SavedPost> builder)
    {
        builder.HasQueryFilter(sp => !sp.IsDeleted);
        
        builder.Property(sp => sp.UserId).HasMaxLength(450);

        builder.HasIndex(sp => new { sp.UserId, sp.PostId }).IsUnique();

        builder.HasOne(sp => sp.Post)
            .WithMany(p => p.SavedPosts)
            .HasForeignKey(sp => sp.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.User)
            .WithMany(u => u.SavedPosts)
            .HasForeignKey(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.HasKey(ph => new { ph.PostId, ph.HashtagId });

        // Index cho phép tra nhanh tất cả PostHashtag theo HashtagId (dùng trong hashtag search)
        builder.HasIndex(ph => ph.HashtagId)
            .HasDatabaseName("IX_PostHashtags_HashtagId");

        builder.HasOne(ph => ph.Post)
            .WithMany(p => p.PostHashtags)
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.HasKey(cp => new { cp.ChatRoomId, cp.UserId });

        builder.HasIndex(cp => cp.UserId);

        builder.HasOne(cp => cp.ChatRoom)
            .WithMany(cr => cr.ChatParticipant)
            .HasForeignKey(cp => cp.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasIndex(m => new { m.ChatRoomId, m.CreatedAt });

        builder.HasOne(m => m.ChatRoom)
            .WithMany(cr => cr.Messages)
            .HasForeignKey(m => m.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasIndex(mr => new { mr.UserId, mr.MessageId }, "IX_Unique_User_Message_Reaction").IsUnique();
        builder.HasQueryFilter(mr => !mr.IsDeleted);

        builder.HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasQueryFilter(n => !n.IsDeleted);

        // Composite index: load notification inbox theo user, sort mới nhất trước
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientId_CreatedAt");

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Hashtag UNIQUE index trên Name:
/// - Tránh duplicate hashtag do race condition (2 post cùng dùng #travel tạo đồng thời).
/// - Cho phép lookup O(1) theo name thay vì Full Table Scan.
/// </summary>
public class HashtagConfiguration : IEntityTypeConfiguration<Hashtag>
{
    public void Configure(EntityTypeBuilder<Hashtag> builder)
    {
        builder.HasIndex(h => h.Name)
            .IsUnique()
            .HasDatabaseName("IX_Hashtags_Name_Unique");
    }
}
