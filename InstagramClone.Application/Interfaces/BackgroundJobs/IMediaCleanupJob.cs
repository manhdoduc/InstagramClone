using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.BackgroundJobs;

public interface IMediaCleanupJob
{
    Task CleanupSoftDeletedMediaAsync(int retentionDays = 30);
}
