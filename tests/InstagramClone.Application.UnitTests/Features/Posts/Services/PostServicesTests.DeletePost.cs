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
    public async Task DeletePostAsync_ShouldReturnSuccess_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockCurrentUserService.Setup(c => c.IsAdmin).Returns(false);

        var post = new InstagramClone.Domain.Entities.Post(userId, "content");
        var media = new InstagramClone.Domain.Entities.PostMedia { MediaUrl = "http://test.com/img.jpg" };
        post.AddMedia(media);

        _mockPostRepository.Setup(r => r.GetByIdWithMediaAsync(postId)).ReturnsAsync(post);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _postServices.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.IsDeleted.Should().BeTrue();
        media.IsDeleted.Should().BeTrue();
        _mockPostRepository.Verify(r => r.Update(post), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_ShouldReturnSuccess_WhenUserIsAdmin()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(adminUserId.ToString());
        _mockCurrentUserService.Setup(c => c.IsAdmin).Returns(true);

        var post = new InstagramClone.Domain.Entities.Post(ownerUserId, "content");
        _mockPostRepository.Setup(r => r.GetByIdWithMediaAsync(postId)).ReturnsAsync(post);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _postServices.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.IsDeleted.Should().BeTrue();
        _mockPostRepository.Verify(r => r.Update(post), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_ShouldReturnForbid_WhenUserIsNotOwnerAndNotAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockCurrentUserService.Setup(c => c.IsAdmin).Returns(false);

        var post = new InstagramClone.Domain.Entities.Post(ownerUserId, "content");
        _mockPostRepository.Setup(r => r.GetByIdWithMediaAsync(postId)).ReturnsAsync(post);

        // Act
        var result = await _postServices.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbid);
        
        _mockPostRepository.Verify(r => r.Update(It.IsAny<InstagramClone.Domain.Entities.Post>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePostAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockPostRepository.Setup(r => r.GetByIdWithMediaAsync(postId)).ReturnsAsync((InstagramClone.Domain.Entities.Post?)null);

        // Act
        var result = await _postServices.DeletePostAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
