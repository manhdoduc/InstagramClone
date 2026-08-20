using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.BackgroundJobs;

public interface IStoryExpirationJob
{
    Task ExpireStoryAsync(Guid storyId);
}
