using FluentAssertions;
using InstagramClone.Application.DTOs.Auth;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace InstagramClone.Tests.Services
{
    public class AuthServicesTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IOptions<JwtSettings>> _mockJwtOptions;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly AuthServices _authService;

        public AuthServicesTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            
            _mockJwtOptions = new Mock<IOptions<JwtSettings>>();
            _mockJwtOptions.Setup(x => x.Value).Returns(new JwtSettings
            {
                Key = "ThisIsASecretKeyForTestingPurposesOnlyItMustBeAtLeast32Bytes",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiryInMinutes = 60,
                RefreshTokenExpiryTime = 7
            });

            _mockCurrentUser = new Mock<ICurrentUserService>();

            _authService = new AuthServices(_mockUserManager.Object, _mockJwtOptions.Object, _mockCurrentUser.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                NickName = "johndoe",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                Password = "Password123"
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), RoleNames.User))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserName.Should().Be(dto.NickName);
            result.Value.Role.Should().Be(RoleNames.User);
        }

        [Fact]
        public async Task RegisterAsync_CreateFails_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                NickName = "johndoe",
                Email = "john@example.com",
                Password = "Password123"
            };

            var identityError = new IdentityError { Description = "Email already in use" };
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.BadRequest && e.Description == "Email already in use");
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokens()
        {
            // Arrange
            var loginDto = new LoginUserDto
            {
                Identifier = "john@example.com",
                Password = "Password123"
            };

            var user = new AppUser { Id = "user-1", Email = "john@example.com", UserName = "johndoe" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Identifier))
                .ReturnsAsync(user);
            
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(true);

            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { RoleNames.User });

            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.AccessToken.Should().NotBeNullOrEmpty();
            result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_InvalidUser_ReturnsFailure()
        {
            // Arrange
            var loginDto = new LoginUserDto { Identifier = "unknown@example.com", Password = "pwd" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Identifier)).ReturnsAsync((AppUser)null);
            _mockUserManager.Setup(x => x.FindByNameAsync(loginDto.Identifier)).ReturnsAsync((AppUser)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.BadRequest);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFailure()
        {
            // Arrange
            var loginDto = new LoginUserDto { Identifier = "john@example.com", Password = "wrongpassword" };
            var user = new AppUser { Id = "user-1", Email = "john@example.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Identifier)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.BadRequest);
        }

        [Fact]
        public async Task LogoutAsync_ValidUser_ClearsRefreshToken()
        {
            // Arrange
            var userId = "user-1";
            var user = new AppUser { Id = userId, RefreshToken = "sometoken", RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7) };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LogoutAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.RefreshToken.Should().BeNull();
            user.RefreshTokenExpiryTime.Should().Be(DateTime.MinValue);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
        }
    }
}
