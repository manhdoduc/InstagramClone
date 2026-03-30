using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;

namespace InstagramClone.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
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



    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tự động filter các record chưa bị xóa (Soft Delete)
        // Lát nữa có Post, Comment cũng sẽ thêm đoạn filter tương tự
        builder.Entity<AppUser>().HasQueryFilter(u => !u.IsDeleted);

        // Tự động bỏ qua các Post và Media đã bị xóa mềm
        builder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<PostMedia>().HasQueryFilter(m => !m.IsDeleted);

        // PHẢI CÓ DÒNG NÀY CHO COMMENT:
        builder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);

        // Nếu đã làm Liked thì cũng thêm luôn:
        builder.Entity<Like>().HasQueryFilter(l => !l.IsDeleted);

        //  
        builder.Entity<Follow>().HasQueryFilter(f => !f.IsDeleted);
        builder.Entity<Follow>(b =>
        {
            // Chặn trường hợp 1 cặp (Observer, Target) xuất hiện 2 lần trong DB
            b.HasIndex(f => new {f.ObserverId, f.TargetId}).IsUnique();

            // Cấu hình nhánh "Người đi follow"
            b.HasOne(f => f.Observer)
                .WithMany(u => u.Followings)
                .HasForeignKey(f => f.ObserverId)
                .OnDelete(DeleteBehavior.Restrict); // QUAN TRỌNG: Không dùng Cascade ở đây

            // Cấu hình nhánh "Người được follow"
            b.HasOne(f => f.Target)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Thiết lập quan hệ (Cascade Delete: Xóa User thì tự động xóa Post)
        builder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostMedia>()
            .HasOne(m => m.Post)
            .WithMany(p => p.MediaPorts)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- CẤU HÌNH BẢNG COMMENT ---
        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade); // Bài Post bay màu -> Comment bay theo

        builder.Entity<Comment>()
            .HasOne(c => c.AppUser)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict); // CHỐNG LỖI CASCADE (Không cho User trực tiếp xóa cứng Comment)

        // --- CẤU HÌNH BẢNG LIKE ---
        builder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Like>()
            .HasOne(l => l.AppUser)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict); // CHỐNG LỖI CASCADE
                                                // Restrict ở đây để tránh trường hợp xóa User sẽ xóa luôn Liked, restrict sẽ báo lỗi nếu cố xóa User mà vẫn còn Liked liên quan, buộc phải xóa Liked trước rồi mới xóa User

        // --- CẤU HÌNH BẢNG COMMENTLIKE ---
        // 1. Áp dụng Filter xóa mềm
        builder.Entity<CommentLike>().HasQueryFilter(cl => !cl.IsDeleted);

        // 2. Cấu hình quan hệ: Xóa Comment thì bay Liked, Xóa User thì không cho xóa ngang
        builder.Entity<CommentLike>()
            .HasOne(cl => cl.Comment)
            .WithMany(c => c.Likes)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommentLike>()
            .HasOne(cl => cl.User)
            .WithMany() // Không cần khai báo ngược lại trong AppUser cho đỡ rối
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Restrict);


        // --- CẤU HÌNH BẢNG SAVEDPOST ---
        // 1. Áp dụng Filter xóa mềm
        builder.Entity<SavedPost>().HasQueryFilter(cl => !cl.IsDeleted);

        // 2. Cấu hình quan hệ: Xóa Post thì bay Saved
        builder.Entity<SavedPost>()
            .HasOne(sp => sp.Post)
            .WithMany()
            .HasForeignKey(sp => sp.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Cấu hình quan hệ: Xóa User thì không cho xóa ngang
        builder.Entity<SavedPost>()
            .HasOne(sp => sp.AppUser)
            .WithMany()
            .HasForeignKey(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CẤU HÌNH BẢNG HASHTAG & POSTHASHTAG ---
        // --- CẤU HÌNH BẢNG POSTHASHTAG ---
        // 1. Khai báo Khóa chính kép
        builder.Entity<PostHashtag>()
            .HasKey(ph => new { ph.PostId, ph.HashtagId });

        // 2. Cấu hình quan hệ rõ ràng để tránh dính lỗi Multiple Cascade Paths
        builder.Entity<PostHashtag>()
            .HasOne(ph => ph.Post)
            .WithMany(p => p.PostHashtags)
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa Post thì xóa luôn thẻ tag gắn trên Post đó

        builder.Entity<PostHashtag>()
            .HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình bảng ChatRoom

        // 1. Cấu hình khóa chính kép cho chatparticipant
        builder.Entity<ChatParticipant>()
            .HasKey(cp => new { cp.ChatRoomId, cp.UserId });

        // 2. quan hệ cho ChatParticipant
        builder.Entity<ChatParticipant>()
            .HasOne(cp => cp.ChatRoom)
            .WithMany(cr => cr.ChatParticipant)
            .HasForeignKey(cp => cp.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ChatParticipant>()
            .HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. quan hệ cho Message
        builder.Entity<Message>()
            .HasOne(m => m.ChatRoom)
            .WithMany(cr => cr.Messages)
            .HasForeignKey(m => m.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Đổi tên các bảng Identity mặc định cho sạch đẹp (tùy chọn)
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");


        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}


