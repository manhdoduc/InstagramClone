using FluentAssertions;
using InstagramClone.Application.DTOs.Common;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using AutoMapper;

namespace InstagramClone.Tests.Services
{
    public class PostServicesTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IStorageServices> _mockStorageServices;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly PostServices _postService;

        private readonly Mock<IGenericRepository<Post>> _mockPostRepo;
        private readonly Mock<IGenericRepository<Like>> _mockLikeRepo;

        public PostServicesTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockStorageServices = new Mock<IStorageServices>();
            _mockCacheService = new Mock<ICacheService>();

            _mockPostRepo = new Mock<IGenericRepository<Post>>();
            _mockLikeRepo = new Mock<IGenericRepository<Like>>();

            _mockUnitOfWork.Setup(u => u.Posts).Returns(_mockPostRepo.Object);
            _mockUnitOfWork.Setup(u => u.Likes).Returns(_mockLikeRepo.Object);

            _postService = new PostServices(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCurrentUser.Object,
                _mockStorageServices.Object,
                _mockCacheService.Object
            );
        }

        [Fact]
        public async Task DeletePostAsync_PostNotFound_ReturnsNotFound()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var posts = new List<Post>().BuildMockDbSet();
            _mockPostRepo.Setup(x => x.Query()).Returns(posts.Object);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }

        [Fact]
        public async Task DeletePostAsync_NotOwner_ReturnsForbid()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var posts = new List<Post> 
            { 
                new Post { Id = postId, UserId = "user-2" } 
            }.BuildMockDbSet();
            
            _mockPostRepo.Setup(x => x.Query()).Returns(posts.Object);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Forbid);
        }

        [Fact]
        public async Task DeletePostAsync_ValidRequest_DeletesPost()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var post = new Post { Id = postId, UserId = userId, IsDeleted = false, MediaItems = new List<PostMedia>() };
            var posts = new List<Post> { post }.BuildMockDbSet();
            
            _mockPostRepo.Setup(x => x.Query()).Returns(posts.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _postService.DeletePostAsync(postId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            post.IsDeleted.Should().BeTrue();
            _mockPostRepo.Verify(x => x.Update(It.IsAny<Post>()), Times.Once);
        }

        [Fact]
        public async Task ToggleLikeAsync_PostNotFound_ReturnsNotFound()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var posts = new List<Post>().BuildMockDbSet();
            _mockPostRepo.Setup(x => x.QueryNoTracking()).Returns(posts.Object);

            // Act
            var result = await _postService.ToggleLikeAsync(postId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }

        [Fact]
        public async Task ToggleLikeAsync_FirstTimeLike_CreatesLike()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = "user-1";
            var postOwnerId = "user-2";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var posts = new List<Post> { new Post { Id = postId, UserId = postOwnerId } }.BuildMockDbSet();
            _mockPostRepo.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Post, bool>>>())).ReturnsAsync(true);
            
            var likes = new List<Like>().BuildMockDbSet();
            _mockLikeRepo.Setup(x => x.Query()).Returns(likes.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _postService.ToggleLikeAsync(postId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockLikeRepo.Verify(x => x.Add(It.Is<Like>(l => l.PostId == postId && l.UserId == userId)), Times.Once);
        }
    }
}
