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
    public async Task DeclineFollowRequestAsync_ShouldReturnSuccess_WhenRequestExists()
    {
        // Arrange
        var userId = Guid.NewGuid(); // Owner
        var followerId = Guid.NewGuid(); // The one who requested
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var request = new Follow(followerId, userId, FollowStatus.Pending);

        _mockUserRepository.Setup(r => r.GetPendingFollowRequestAsync(followerId, userId)).ReturnsAsync(request);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _followServices.DeclineFollowRequestAsync(followerId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.IsDeleted.Should().BeTrue();

        _mockUserRepository.Verify(r => r.UpdateFollow(request), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        
        // Ensure NO notification is sent for decline
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task DeclineFollowRequestAsync_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        _mockUserRepository.Setup(r => r.GetPendingFollowRequestAsync(followerId, userId)).ReturnsAsync((Follow?)null);

        // Act
        var result = await _followServices.DeclineFollowRequestAsync(followerId.ToString());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
