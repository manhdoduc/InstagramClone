using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.BackgroundJobs.Jobs;

public class StoryExpirationJob(
    AppDbContext context,
    ICacheService cache,
    ILogger<StoryExpirationJob> logger) : IStoryExpirationJob
{
    public async Task ExpireStoryAsync(Guid storyId)
    {
        // 1. Tìm story (kể cả khi đã qua query filter)
        var story = await context.Stories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == storyId);

        if (story == null || story.IsDeleted)
        {
            return;
        }

        // 2. Đánh dấu hết hạn (Soft-delete)
        story.Expire();
        await context.SaveChangesAsync();

        // 3. Làm mới cache
        await cache.BumpScopeVersionAsync($"stories:user:{story.UserId}");
        await cache.BumpScopeVersionAsync("stories:feed:version");

        logger.LogInformation("Story {StoryId} of user {UserId} automatically expired after 24h.", storyId, story.UserId);
    }
}
