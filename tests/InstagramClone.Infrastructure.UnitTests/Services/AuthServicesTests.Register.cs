using FluentAssertions;
using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Infrastructure.UnitTests.Services;

public partial class AuthServicesTests
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            NickName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleNames.User))
            .ReturnsAsync(IdentityResult.Success);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authServices.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(dto.Email);
        result.Value.UserName.Should().Be(dto.NickName);

        _mockUserRepository.Verify(r => r.Add(It.IsAny<AppUser>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnBadRequest_WhenCreateUserFails()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var errors = new[] { new IdentityError { Description = "Email is already taken." } };
        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _authServices.RegisterAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Description.Should().Be("Email is already taken.");
        
        _mockUserRepository.Verify(r => r.Add(It.IsAny<AppUser>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRollbackIdentityUser_WhenProfileCreationFails()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
            
        _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleNames.User))
            .ReturnsAsync(IdentityResult.Success);

        // Simulate DB failure
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var act = async () => await _authServices.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        
        // Ensure that rollback (DeleteAsync) was called
        _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }
}
