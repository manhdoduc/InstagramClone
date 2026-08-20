using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.BackgroundJobs;

public interface ITokenCleanupJob
{
    Task CleanupExpiredRefreshTokensAsync();
}
