using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.BackgroundJobs.Jobs;

public class TokenCleanupJob(AppDbContext context, ILogger<TokenCleanupJob> logger) : ITokenCleanupJob
{
    public async Task CleanupExpiredRefreshTokensAsync()
    {
        var now = DateTime.UtcNow;
        
        var affectedRows = await context.AppUsers
            .Where(u => u.RefreshToken != null && u.RefreshTokenExpiryTime <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.RefreshToken, (string?)null)
                .SetProperty(u => u.RefreshTokenExpiryTime, DateTime.MinValue));

        logger.LogInformation("Expired refresh token cleanup completed. Purged {Count} expired tokens.", affectedRows);
    }
}
