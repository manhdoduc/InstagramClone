using FluentAssertions;
using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Infrastructure.UnitTests.Services;

public partial class AuthServicesTests
{
    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var dto = new LoginUserDto { Identifier = "test@example.com", Password = "Password123!" };
        var applicationUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
        var appUser = new AppUser(applicationUser.Id, "testuser", "test@example.com", "First", "Last", "First Last");

        _mockUserManager.Setup(m => m.FindByEmailAsync(dto.Identifier)).ReturnsAsync(applicationUser);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(applicationUser, dto.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.GetRolesAsync(applicationUser)).ReturnsAsync(new List<string> { RoleNames.User });
        
        _mockUserRepository.Setup(r => r.GetByIdAsync(applicationUser.Id)).ReturnsAsync(appUser);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authServices.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();

        _mockUserRepository.Verify(r => r.Update(It.Is<AppUser>(u => u.RefreshToken != null)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldCreateAppUser_WhenProfileIsMissing()
    {
        // Arrange
        var dto = new LoginUserDto { Identifier = "testuser", Password = "Password123!" };
        var applicationUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };

        _mockUserManager.Setup(m => m.FindByEmailAsync(dto.Identifier)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(m => m.FindByNameAsync(dto.Identifier)).ReturnsAsync(applicationUser);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(applicationUser, dto.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.GetRolesAsync(applicationUser)).ReturnsAsync(new List<string> { RoleNames.User });
        
        // Return null to simulate missing AppUser
        _mockUserRepository.Setup(r => r.GetByIdAsync(applicationUser.Id)).ReturnsAsync((AppUser?)null);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authServices.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify Add was called to create the missing profile
        _mockUserRepository.Verify(r => r.Add(It.IsAny<AppUser>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for Add, once for UpdateRefreshToken
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnBadRequest_WhenUserNotFound()
    {
        // Arrange
        var dto = new LoginUserDto { Identifier = "invalid@example.com", Password = "Password123!" };
        
        _mockUserManager.Setup(m => m.FindByEmailAsync(dto.Identifier)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(m => m.FindByNameAsync(dto.Identifier)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authServices.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnBadRequest_WhenPasswordIsIncorrect()
    {
        // Arrange
        var dto = new LoginUserDto { Identifier = "test@example.com", Password = "WrongPassword!" };
        var applicationUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };

        _mockUserManager.Setup(m => m.FindByEmailAsync(dto.Identifier)).ReturnsAsync(applicationUser);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(applicationUser, dto.Password)).ReturnsAsync(false);

        // Act
        var result = await _authServices.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
    }
}
