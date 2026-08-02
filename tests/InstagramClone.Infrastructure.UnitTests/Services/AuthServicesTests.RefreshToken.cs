using FluentAssertions;
using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Identity;
using Moq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Infrastructure.UnitTests.Services;

public partial class AuthServicesTests
{
    private string GenerateTestJwtToken(string userId, string email, int expiryInMinutes)
    {
        var sercurityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("a_very_long_secret_key_that_is_at_least_32_bytes_long"));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(sercurityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new (Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, userId),
            new (Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email, email)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnSuccess_WhenTokensAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        
        // Generate an expired token
        var expiredToken = GenerateTestJwtToken(userId, email, -10);
        var oldRefreshToken = "old-refresh-token";
        
        var dto = new TokenResponseDto { AccessToken = expiredToken, RefreshToken = oldRefreshToken };

        var applicationUser = new ApplicationUser { Id = Guid.Parse(userId), Email = email, UserName = "testuser" };
        var appUser = new AppUser(Guid.Parse(userId), "testuser", email, "First", "Last", "First Last");
        
        appUser.UpdateRefreshToken(HashToken(oldRefreshToken), DateTime.UtcNow.AddDays(1)); // Valid refresh token

        _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(applicationUser);
        _mockUserManager.Setup(m => m.GetRolesAsync(applicationUser)).ReturnsAsync(new System.Collections.Generic.List<string>());
        _mockUserRepository.Setup(r => r.GetByIdAsync(Guid.Parse(userId))).ReturnsAsync(appUser);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authServices.RefreshTokenAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.AccessToken.Should().NotBe(expiredToken);
        result.Value.RefreshToken.Should().NotBe(oldRefreshToken);

        _mockUserRepository.Verify(r => r.Update(It.IsAny<AppUser>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnBadRequest_WhenAccessTokenIsInvalid()
    {
        // Arrange
        var dto = new TokenResponseDto { AccessToken = "invalid-token", RefreshToken = "refresh" };

        // Act
        var result = await _authServices.RefreshTokenAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
        result.Errors[0].Description.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnBadRequest_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var expiredToken = GenerateTestJwtToken(userId, email, -10);
        var oldRefreshToken = "old-refresh-token";
        
        var dto = new TokenResponseDto { AccessToken = expiredToken, RefreshToken = oldRefreshToken };

        var applicationUser = new ApplicationUser { Id = Guid.Parse(userId), Email = email, UserName = "testuser" };
        var appUser = new AppUser(Guid.Parse(userId), "testuser", email, "First", "Last", "First Last");
        
        // Expired refresh token
        appUser.UpdateRefreshToken(HashToken(oldRefreshToken), DateTime.UtcNow.AddDays(-1)); 

        _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(applicationUser);
        _mockUserRepository.Setup(r => r.GetByIdAsync(Guid.Parse(userId))).ReturnsAsync(appUser);

        // Act
        var result = await _authServices.RefreshTokenAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.BadRequest);
        result.Errors[0].Description.Should().Contain("Invalid refresh token");
    }
}
