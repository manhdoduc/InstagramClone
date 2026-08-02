using FluentAssertions;
using InstagramClone.Common.Constants;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    [Fact]
    public async Task UpdatePostAsync_ShouldReturnSuccess_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var post = new InstagramClone.Domain.Entities.Post(userId, "old content");
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _postServices.UpdatePostAsync("new content", postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.Content.Should().Be("new content");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldReturnForbid_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var post = new InstagramClone.Domain.Entities.Post(ownerUserId, "old content");
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync(post);

        // Act
        var result = await _postServices.UpdatePostAsync("new content", postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbid);
        
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockPostRepository.Setup(r => r.GetByIdAsync(postId)).ReturnsAsync((InstagramClone.Domain.Entities.Post?)null);

        // Act
        var result = await _postServices.UpdatePostAsync("new content", postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
