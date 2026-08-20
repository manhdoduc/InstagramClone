using FluentAssertions;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Application.UnitTests.Features.Follows.Services;

public partial class FollowServicesTests
{
    [Fact]
    public async Task SendFollowRequestAsync_ShouldReturnSuccess_WhenTargetIsPublic_AndFirstTimeFollowing()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(followerId.ToString());

        var targetUser = new AppUser(followeeId, "target", "test@test.com", "Test", "User", "Test User");
        targetUser.SetAccountPrivacy(false); // Public

        _mockUserRepository.Setup(r => r.GetByIdAsync(followeeId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(r => r.GetFollowAsync(followerId, followeeId, true)).ReturnsAsync((Follow?)null);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _followServices.SendFollowRequestAsync(followeeId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(FollowCodes.Followed);

        _mockUserRepository.Verify(r => r.AddFollow(It.Is<Follow>(f => f.Status == FollowStatus.Accepted)), Times.Once);
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Once);
        _mockCacheService.Verify(c => c.BumpScopeVersionAsync(It.Is<string>(s => s.Contains(followeeId.ToString()))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendFollowRequestAsync_ShouldReturnSuccess_WhenTargetIsPrivate_AndFirstTimeFollowing()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(followerId.ToString());

        var targetUser = new AppUser(followeeId, "target", "test@test.com", "Test", "User", "Test User");
        targetUser.SetAccountPrivacy(true); // Private

        _mockUserRepository.Setup(r => r.GetByIdAsync(followeeId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(r => r.GetFollowAsync(followerId, followeeId, true)).ReturnsAsync((Follow?)null);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _followServices.SendFollowRequestAsync(followeeId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(FollowCodes.FollowRequestSent);

        _mockUserRepository.Verify(r => r.AddFollow(It.Is<Follow>(f => f.Status == FollowStatus.Pending)), Times.Once);
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendFollowRequestAsync_ShouldUnfollow_WhenAlreadyFollowing()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(followerId.ToString());

        var targetUser = new AppUser(followeeId, "target", "test@test.com", "Test", "User", "Test User");
        var existingFollow = new Follow(followerId, followeeId, FollowStatus.Accepted); // Active

        _mockUserRepository.Setup(r => r.GetByIdAsync(followeeId)).ReturnsAsync(targetUser);
        _mockUserRepository.Setup(r => r.GetFollowAsync(followerId, followeeId, true)).ReturnsAsync(existingFollow);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _followServices.SendFollowRequestAsync(followeeId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(FollowCodes.CancelledFollowRequest);
        existingFollow.IsDeleted.Should().BeTrue();

        _mockUserRepository.Verify(r => r.UpdateFollow(existingFollow), Times.Once);
        // Ensure no new notification is sent for unfollowing
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendFollowRequestAsync_ShouldReturnConflict_WhenFollowingYourself()
    {
        // Arrange
        var selfId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(selfId.ToString());

        // Act
        var result = await _followServices.SendFollowRequestAsync(selfId.ToString());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Fact]
    public async Task SendFollowRequestAsync_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(followerId.ToString());

        _mockUserRepository.Setup(r => r.GetByIdAsync(followeeId)).ReturnsAsync((AppUser?)null);

        // Act
        var result = await _followServices.SendFollowRequestAsync(followeeId.ToString());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
