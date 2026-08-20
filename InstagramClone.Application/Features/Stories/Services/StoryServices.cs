using InstagramClone.Application.Features.Stories.DTOs;
using InstagramClone.Application.Interfaces.BackgroundJobs;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Stories.Services;

public class StoryServices(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IStorageServices storageServices,
    ICacheService cache,
    IBackgroundJobService backgroundJobService,
    IOptions<MediaSettings> mediaSettingsOptions
    ) : IStoryServices
{
    private readonly MediaSettings _mediaSettings = mediaSettingsOptions.Value;

    public async Task<Result<Guid>> CreateStoryAsync(CreateStoryDto dto)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            return Result<Guid>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return Result<Guid>.NotFound(new Error(ErrorCodes.NotFound, "User not found"));

        // 1. Tải lên 1 ảnh duy nhất vào folder "stories"
        var uploadResult = await storageServices.UploadImageAsync(
            dto.File,
            userId.ToString(),
            "stories",
            _mediaSettings.Story.MaxWidth,
            _mediaSettings.Story.MaxHeight);

        if (!uploadResult.IsSuccess)
        {
            return Result<Guid>.Failure(uploadResult.Errors);
        }

        var mediaUrl = uploadResult.Value!;

        // 2. Tạo bản ghi Story trong Database
        var story = new Story(userId, mediaUrl, dto.Caption);
        unitOfWork.Stories.Add(story);

        try
        {
            var saved = await unitOfWork.SaveChangesAsync() > 0;
            if (!saved)
            {
                await storageServices.DeleteFile(mediaUrl); // Rollback file nếu DB save lỗi
                return Result<Guid>.Failure(new Error(ErrorCodes.Failure, "Failed to save story to database."));
            }

            Log.Information("User {UserId} created story {StoryId}, valid until {ExpiresAt}", userId, story.Id, story.ExpiresAt);

            // 3. Sử dụng Hangfire Delayed Job duy trì chính xác 24h
            backgroundJobService.Schedule<IStoryExpirationJob>(
                job => job.ExpireStoryAsync(story.Id),
                TimeSpan.FromHours(24));

            // 4. Invalidate stories cache
            await cache.BumpScopeVersionAsync($"stories:user:{userId}");
            await cache.BumpScopeVersionAsync("stories:feed:version");

            return Result<Guid>.Success(story.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating story for user {UserId}", userId);
            await storageServices.DeleteFile(mediaUrl);
            return Result<Guid>.Failure(new Error(ErrorCodes.Failure, "An error occurred while creating story."));
        }
    }

    public async Task<Result<List<UserStoryFeedDto>>> GetStoriesFeedAsync()
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
            return Result<List<UserStoryFeedDto>>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        // Lấy danh sách người mình đang follow + chính mình
        var targetUserIds = await unitOfWork.Users.GetFollowingIdsAsync(currentUserId);
        targetUserIds.Add(currentUserId);

        var activeStories = await unitOfWork.Stories.GetActiveStoriesByUserIdsAsync(targetUserIds);

        // Gom nhóm theo từng User
        var groupedStories = activeStories
            .GroupBy(s => s.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                return new UserStoryFeedDto
                {
                    UserId = g.Key,
                    UserName = user?.UserName ?? string.Empty,
                    AvatarUrl = user?.AvatarUrl,
                    HasUnseenStory = true,
                    Stories = g.Select(s => new StoryDto
                    {
                        Id = s.Id,
                        MediaUrl = s.MediaUrl,
                        Caption = s.Caption,
                        CreatedAt = s.CreatedAt,
                        ExpiresAt = s.ExpiresAt,
                        IsMyStory = s.UserId == currentUserId
                    }).OrderBy(s => s.CreatedAt).ToList()
                };
            })
            // Đưa story của chính mình lên đầu tiên (giống Instagram)
            .OrderByDescending(g => g.UserId == currentUserId)
            .ToList();

        return Result<List<UserStoryFeedDto>>.Success(groupedStories);
    }

    public async Task<Result<List<StoryDto>>> GetUserStoriesAsync(Guid targetUserId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
            return Result<List<StoryDto>>.BadRequest(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var stories = await unitOfWork.Stories.GetActiveStoriesByUserIdAsync(targetUserId);

        var storyDtos = stories.Select(s => new StoryDto
        {
            Id = s.Id,
            MediaUrl = s.MediaUrl,
            Caption = s.Caption,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            IsMyStory = s.UserId == currentUserId
        }).OrderBy(s => s.CreatedAt).ToList();

        return Result<List<StoryDto>>.Success(storyDtos);
    }

    public async Task<Result> DeleteStoryAsync(Guid storyId)
    {
        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
            return Result.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

        var story = await unitOfWork.Stories.GetByIdAsync(storyId);
        if (story == null)
            return Result.NotFound(new Error(ErrorCodes.NotFound, "Story not found"));

        if (story.UserId != currentUserId && !currentUser.IsAdmin)
            return Result.Failure(new Error(ErrorCodes.Forbid, "You are not authorized to delete this story"));

        story.Expire();
        unitOfWork.Stories.Update(story);

        var saved = await unitOfWork.SaveChangesAsync() > 0;
        if (saved)
        {
            Log.Information("User {UserId} deleted story {StoryId}", currentUserId, storyId);
            
            // Xóa file vật lý qua background job
            if (!string.IsNullOrWhiteSpace(story.MediaUrl))
            {
                backgroundJobService.Schedule<IStorageServices>(
                    svc => svc.DeleteFile(story.MediaUrl),
                    TimeSpan.FromSeconds(5));
            }

            await cache.BumpScopeVersionAsync($"stories:user:{story.UserId}");
            await cache.BumpScopeVersionAsync("stories:feed:version");

            return Result.Success();
        }

        return Result.Failure(new Error(ErrorCodes.Failure, "Failed to delete story."));
    }
}
