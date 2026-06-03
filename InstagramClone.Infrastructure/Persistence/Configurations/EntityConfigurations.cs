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

        builder.HasIndex(f => new { f.FollowerId, f.FolloweeId }).IsUnique();

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
