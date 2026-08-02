using FluentAssertions;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Common.Constants;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnSuccess_WhenPostExists()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var expectedPost = new ResponsePostDto { Id = postId, Content = "Test Post" };

        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockCacheService.Setup(c => c.GetScopeVersionAsync(It.IsAny<string>())).ReturnsAsync("1");
        
        // Setup cache to execute the factory if needed, or just return the object directly
        // Because cache.GetOrCreateAsync is used, we can mock it to just return expectedPost
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<ResponsePostDto?>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(expectedPost);

        // Act
        var result = await _postServices.GetPostByIdAsync(postId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(postId);
        result.Value.Content.Should().Be("Test Post");
    }

    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockCacheService.Setup(c => c.GetScopeVersionAsync(It.IsAny<string>())).ReturnsAsync("1");
        
        // Mock cache to return null
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<ResponsePostDto?>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((ResponsePostDto?)null);

        // Act
        var result = await _postServices.GetPostByIdAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnBadRequest_WhenUserIdIsInvalid()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns("invalid-guid");

        // Act
        var result = await _postServices.GetPostByIdAsync(postId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
    }
}
