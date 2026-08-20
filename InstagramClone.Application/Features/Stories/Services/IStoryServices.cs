using InstagramClone.Application.Features.Stories.DTOs;
using InstagramClone.Common.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Stories.Services;

public interface IStoryServices
{
    Task<Result<Guid>> CreateStoryAsync(CreateStoryDto dto);
    Task<Result<List<UserStoryFeedDto>>> GetStoriesFeedAsync();
    Task<Result<List<StoryDto>>> GetUserStoriesAsync(Guid targetUserId);
    Task<Result> DeleteStoryAsync(Guid storyId);
}
