using FluentAssertions;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Common.Results;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    [Fact]
    public async Task CreatePostAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var dto = new CreatePostDto
        {
            Content = "Test caption",
            Files = new System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile> { fileMock.Object }
        };

        _mockStorageServices
            .Setup(s => s.UploadImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<string>.Success("http://test.com/image.jpg"));

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _postServices.CreatePostAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        Guid.TryParse(result.Value, out _).Should().BeTrue();

        _mockPostRepository.Verify(r => r.Add(It.IsAny<InstagramClone.Domain.Entities.Post>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldReturnFailure_WhenUploadFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var dto = new CreatePostDto
        {
            Content = "Test caption",
            Files = new System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile> { fileMock.Object }
        };

        _mockStorageServices
            .Setup(s => s.UploadImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result<string>.Failure(new Error("UploadError", "Upload failed")));

        // Act
        var result = await _postServices.CreatePostAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Description.Should().Contain("Failed to upload media");

        _mockPostRepository.Verify(r => r.Add(It.IsAny<InstagramClone.Domain.Entities.Post>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
