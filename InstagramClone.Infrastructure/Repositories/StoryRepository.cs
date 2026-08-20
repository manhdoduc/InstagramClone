using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.Repositories;

public class StoryRepository(AppDbContext context) : IStoryRepository
{
    public void Add(Story story)
    {
        context.Stories.Add(story);
    }

    public void Update(Story story)
    {
        context.Stories.Update(story);
    }

    public async Task<Story?> GetByIdAsync(Guid id)
    {
        return await context.Stories.FindAsync(id);
    }

    public async Task<List<Story>> GetActiveStoriesByUserIdsAsync(IEnumerable<Guid> userIds)
    {
        return await context.Stories
            .Include(s => s.User)
            .Where(s => userIds.Contains(s.UserId))
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Story>> GetActiveStoriesByUserIdAsync(Guid userId)
    {
        return await context.Stories
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }
}
