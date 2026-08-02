using FluentAssertions;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Common.Constants;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnSuccess_WhenAccountIsPublic()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var targetUser = new InstagramClone.Domain.Entities.AppUser(targetUserId, "target", "target@test.com", "first", "last", "first last");
        targetUser.SetAccountPrivacy(false);

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockCacheService.Setup(c => c.GetScopeVersionAsync(It.IsAny<string>())).ReturnsAsync("1");

        var pagedResponse = new CursorPagedResponse<ResponsePostDto> { Items = new List<ResponsePostDto>() };
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<CursorPagedResponse<ResponsePostDto>>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _postServices.GetUserPostsAsync(targetUserId.ToString(), new CursorPaginationRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnSuccess_WhenAccountIsPrivateAndUserIsFollower()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var targetUser = new InstagramClone.Domain.Entities.AppUser(targetUserId, "target", "target@test.com", "first", "last", "first last");
        targetUser.SetAccountPrivacy(true);

        var follow = new InstagramClone.Domain.Entities.Follow(currentUserId, targetUserId, InstagramClone.Domain.Enums.FollowStatus.Accepted);

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(r => r.GetFollowAsync(currentUserId, targetUserId)).ReturnsAsync(follow);
        
        _mockCacheService.Setup(c => c.GetScopeVersionAsync(It.IsAny<string>())).ReturnsAsync("1");

        var pagedResponse = new CursorPagedResponse<ResponsePostDto> { Items = new List<ResponsePostDto>() };
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<CursorPagedResponse<ResponsePostDto>>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _postServices.GetUserPostsAsync(targetUserId.ToString(), new CursorPaginationRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnForbid_WhenAccountIsPrivateAndUserIsNotFollower()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var targetUser = new InstagramClone.Domain.Entities.AppUser(targetUserId, "target", "target@test.com", "first", "last", "first last");
        targetUser.SetAccountPrivacy(true);

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(r => r.GetFollowAsync(currentUserId, targetUserId)).ReturnsAsync((InstagramClone.Domain.Entities.Follow?)null);

        // Act
        var result = await _postServices.GetUserPostsAsync(targetUserId.ToString(), new CursorPaginationRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbid);
    }

    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync((InstagramClone.Domain.Entities.AppUser?)null);

        // Act
        var result = await _postServices.GetUserPostsAsync(targetUserId.ToString(), new CursorPaginationRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
