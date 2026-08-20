using InstagramClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace InstagramClone.Infrastructure.Persistence.Configurations;

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        // Global Query Filter: Chỉ lấy story chưa bị xóa và còn hạn trong 24 giờ
        builder.HasQueryFilter(s => !s.IsDeleted && s.ExpiresAt > DateTime.UtcNow);

        builder.Property(s => s.MediaUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Caption)
            .HasMaxLength(500);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.ExpiresAt });
    }
}
