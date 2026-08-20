using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.BackgroundJobs.Jobs;

public class MediaCleanupJob(
    AppDbContext context,
    IStorageServices storageServices,
    ILogger<MediaCleanupJob> logger) : IMediaCleanupJob
{
    public async Task CleanupSoftDeletedMediaAsync(int retentionDays = 30)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-retentionDays);

        // Lấy danh sách các media bị soft-deleted quá thời gian retention
        var expiredMediaList = await context.PostMedias
            .IgnoreQueryFilters()
            .Where(m => m.IsDeleted && (m.UpdatedAt ?? m.CreatedAt) <= thresholdDate)
            .Take(100) // Batch processing theo từng đợt để tối ưu bộ nhớ
            .ToListAsync();

        if (expiredMediaList.Count == 0)
        {
            logger.LogInformation("No soft-deleted media files to purge (retention: {Days} days).", retentionDays);
            return;
        }

        int deletedFileCount = 0;

        foreach (var media in expiredMediaList)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(media.MediaUrl))
                {
                    await storageServices.DeleteFile(media.MediaUrl);
                    deletedFileCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete physical media file: {MediaUrl}", media.MediaUrl);
            }
        }

        // Xóa vĩnh viễn các bản ghi media khỏi Database
        context.PostMedias.RemoveRange(expiredMediaList);
        await context.SaveChangesAsync();

        logger.LogInformation("Soft-deleted media cleanup completed. Purged {DbCount} DB records and {FileCount} physical files.",
            expiredMediaList.Count, deletedFileCount);
    }
}
