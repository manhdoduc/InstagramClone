using FluentAssertions;
using InstagramClone.Common.Constants;
using InstagramClone.Application.Features.Notifications.Services;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    [Fact]
    public async Task ToggleLikeAsync_ShouldAddNewLike_WhenLikeDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var postOwnerId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        
        var post = new InstagramClone.Domain.Entities.Post(postOwnerId, "content");
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(r => r.GetLikeAsync(userId, postId, true)).ReturnsAsync((InstagramClone.Domain.Entities.Like?)null);

        // Act
        var result = await _postServices.ToggleLikeAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockPostRepository.Verify(r => r.AddLike(It.IsAny<InstagramClone.Domain.Entities.Like>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task ToggleLikeAsync_ShouldRemoveLikeAndNotNotify_WhenLikeExistsAndIsActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var postOwnerId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        
        var post = new InstagramClone.Domain.Entities.Post(postOwnerId, "content");
        var existingLike = new InstagramClone.Domain.Entities.Like(userId, postId); // Default is active (IsDeleted = false)
        
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(r => r.GetLikeAsync(userId, postId, true)).ReturnsAsync(existingLike);

        // Act
        var result = await _postServices.ToggleLikeAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingLike.IsDeleted.Should().BeTrue(); // Should have been toggled
        _mockPostRepository.Verify(r => r.UpdateLike(existingLike), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockBackgroundJobService.Verify(b => b.Enqueue(It.IsAny<System.Linq.Expressions.Expression<Func<INotificationServices, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task ToggleLikeAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync((InstagramClone.Domain.Entities.Post?)null);

        // Act
        var result = await _postServices.ToggleLikeAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        
        _mockPostRepository.Verify(r => r.AddLike(It.IsAny<InstagramClone.Domain.Entities.Like>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
