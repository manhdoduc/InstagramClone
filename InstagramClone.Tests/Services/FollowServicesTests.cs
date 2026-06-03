using FluentAssertions;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Infrastructure.Services;
using InstagramClone.Application.Features.Chat.Services;
using InstagramClone.Application.Features.Comments.Services;
using InstagramClone.Application.Features.Follows.Services;
using InstagramClone.Application.Features.Posts.Services;
using InstagramClone.Infrastructure.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Moq;
using System.Linq.Expressions;
using AutoMapper;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Users.DTOs;
using MockQueryable.Moq;
using Microsoft.EntityFrameworkCore;
namespace InstagramClone.Tests.Services
{
    public class FollowServicesTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly FollowServices _followService;
        
        private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
        private readonly Mock<IGenericRepository<Follow>> _mockFollowRepo;

        public FollowServicesTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMapper = new Mock<IMapper>();

            _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
            _mockFollowRepo = new Mock<IGenericRepository<Follow>>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.Follows).Returns(_mockFollowRepo.Object);

            _followService = new FollowServices(
                _mockUnitOfWork.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task SendFollowRequestAsync_FollowSelf_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

            // Act
            var result = await _followService.SendFollowRequestAsync(userId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        }

        [Fact]
        public async Task SendFollowRequestAsync_TargetNotFound_ReturnsNotFound()
        {
            // Arrange
            var FollowerId = "user-1";
            var FolloweeId = "user-2";
            _mockCurrentUser.Setup(x => x.UserId).Returns(FollowerId);
            
            var users = new List<AppUser>().BuildMockDbSet();
            _mockUserRepo.Setup(x => x.QueryNoTracking()).Returns(users.Object);

            // Act
            var result = await _followService.SendFollowRequestAsync(FolloweeId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }

        [Fact]
        public async Task SendFollowRequestAsync_PublicAccount_ReturnsSuccessAndAccepted()
        {
            // Arrange
            var FollowerId = "user-1";
            var FolloweeId = "user-2";
            _mockCurrentUser.Setup(x => x.UserId).Returns(FollowerId);

            var users = new List<AppUser> 
            { 
                new AppUser { Id = FolloweeId, IsPrivateAccount = false } 
            }.BuildMockDbSet();
            
            _mockUserRepo.Setup(x => x.QueryNoTracking()).Returns(users.Object);
            
            var follows = new List<Follow>().BuildMockDbSet();
            _mockFollowRepo.Setup(x => x.Query()).Returns(follows.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _followService.SendFollowRequestAsync(FolloweeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(FollowCodes.Followed);
            _mockFollowRepo.Verify(x => x.Add(It.Is<Follow>(f => f.FollowerId == FollowerId && f.FolloweeId == FolloweeId && f.Status == FollowStatus.Accepted)), Times.Once);
        }

        [Fact]
        public async Task SendFollowRequestAsync_PrivateAccount_ReturnsSuccessAndPending()
        {
            // Arrange
            var FollowerId = "user-1";
            var FolloweeId = "user-2";
            _mockCurrentUser.Setup(x => x.UserId).Returns(FollowerId);

            var users = new List<AppUser> 
            { 
                new AppUser { Id = FolloweeId, IsPrivateAccount = true } 
            }.BuildMockDbSet();
            
            _mockUserRepo.Setup(x => x.QueryNoTracking()).Returns(users.Object);
            
            var follows = new List<Follow>().BuildMockDbSet();
            _mockFollowRepo.Setup(x => x.Query()).Returns(follows.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _followService.SendFollowRequestAsync(FolloweeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(FollowCodes.FollowRequestSent);
            _mockFollowRepo.Verify(x => x.Add(It.Is<Follow>(f => f.FollowerId == FollowerId && f.FolloweeId == FolloweeId && f.Status == FollowStatus.Pending)), Times.Once);
        }
    }
}
