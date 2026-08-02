using FluentAssertions;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Infrastructure.UnitTests.Services;

public partial class AuthServicesTests
{
    [Fact]
    public async Task LogoutAsync_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

        var appUser = new AppUser(userId, "test", "test@example.com", "Test", "User", "Test User");
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(appUser);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authServices.LogoutAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Assert refresh token is cleared
        appUser.RefreshToken.Should().BeNull();
        appUser.RefreshTokenExpiryTime.Should().Be(DateTime.MinValue);

        _mockUserRepository.Verify(r => r.Update(appUser), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnBadRequest_WhenUserIdIsInvalid()
    {
        // Arrange
        _mockCurrentUserService.Setup(c => c.UserId).Returns("invalid-guid");

        // Act
        var result = await _authServices.LogoutAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((AppUser?)null);

        // Act
        var result = await _authServices.LogoutAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
