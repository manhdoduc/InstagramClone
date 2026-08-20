using InstagramClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface IStoryRepository
{
    void Add(Story story);
    void Update(Story story);
    Task<Story?> GetByIdAsync(Guid id);
    Task<List<Story>> GetActiveStoriesByUserIdsAsync(IEnumerable<Guid> userIds);
    Task<List<Story>> GetActiveStoriesByUserIdAsync(Guid userId);
}
