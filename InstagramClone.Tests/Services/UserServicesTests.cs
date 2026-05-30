using FluentAssertions;
using InstagramClone.Application.DTOs.InfoUser;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using AutoMapper;
using System.Linq.Expressions;

namespace InstagramClone.Tests.Services
{
    public class UserServicesTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IStorageServices> _mockStorageServices;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly UserServices _userServices;
        private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
        private readonly Mock<IGenericRepository<Post>> _mockPostRepo;

        public UserServicesTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockStorageServices = new Mock<IStorageServices>();
            _mockCacheService = new Mock<ICacheService>();

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
            _mockPostRepo = new Mock<IGenericRepository<Post>>();
            
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.Posts).Returns(_mockPostRepo.Object);

            _userServices = new UserServices(
                _mockStorageServices.Object,
                _mockUserManager.Object,
                _mockCurrentUser.Object,
                _mockUnitOfWork.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task UploadAvatarAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockCurrentUser.Setup(x => x.UserId).Returns("user-1");
            _mockUserManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync((AppUser)null!);
            var mockFile = new Mock<IFormFile>();

            // Act
            var result = await _userServices.UploadAvatarAsync(mockFile.Object);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }

        [Fact]
        public async Task ToggleAccountPrivacyAsync_ValidRequest_TogglesPrivacy()
        {
            // Arrange
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            var user = new AppUser { Id = userId, IsPrivateAccount = false };
            
            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userServices.ToggleAccountPrivacyAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.IsPrivateAccount.Should().BeTrue();
            result.Value.Should().Contain(PrivateAccounts.Private);
        }

        [Fact]
        public async Task UploadBioAsync_ValidRequest_UpdatesBio()
        {
            // Arrange
            var userId = "user-1";
            var bio = "New Bio";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            var user = new AppUser { Id = userId, Bio = "" };
            
            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userServices.UploadBioAsync(bio);

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.Bio.Should().Be(bio);
        }

        [Fact]
        public async Task SearchUsersAsync_EmptySearchTerm_ReturnsBadRequest()
        {
            // Arrange
            _mockCurrentUser.Setup(x => x.UserId).Returns("user-1");

            // Act
            var result = await _userServices.SearchUsersAsync("");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.BadRequest);
        }
    }
}
