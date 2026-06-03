using FluentAssertions;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
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
using MockQueryable.Moq;
using Moq;
using AutoMapper;

namespace InstagramClone.Tests.Services
{
    public class CommentServicesTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CommentServices _commentService;

        private readonly Mock<IGenericRepository<Comment>> _mockCommentRepo;
        private readonly Mock<IGenericRepository<Post>> _mockPostRepo;
        private readonly Mock<IGenericRepository<CommentLike>> _mockCommentLikeRepo;

        public CommentServicesTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            _mockCommentRepo = new Mock<IGenericRepository<Comment>>();
            _mockPostRepo = new Mock<IGenericRepository<Post>>();
            _mockCommentLikeRepo = new Mock<IGenericRepository<CommentLike>>();

            _mockUnitOfWork.Setup(u => u.Comments).Returns(_mockCommentRepo.Object);
            _mockUnitOfWork.Setup(u => u.Posts).Returns(_mockPostRepo.Object);
            _mockUnitOfWork.Setup(u => u.CommentLikes).Returns(_mockCommentLikeRepo.Object);

            _commentService = new CommentServices(
                _mockUnitOfWork.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task DeleteCommentAsync_CommentNotFound_ReturnsNotFound()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var comments = new List<Comment>().BuildMockDbSet();
            _mockCommentRepo.Setup(x => x.Query()).Returns(comments.Object);

            // Act
            var result = await _commentService.DeleteCommentAsync(commentId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }

        [Fact]
        public async Task DeleteCommentAsync_NotAuthorOrPostOwner_ReturnsForbid()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = "user-1"; // the current user (trying to delete)
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var comment = new Comment 
            { 
                Id = commentId, 
                UserId = "user-2", // comment author
                Post = new Post { UserId = "user-3" } // post owner
            };
            var comments = new List<Comment> { comment }.BuildMockDbSet();
            _mockCommentRepo.Setup(x => x.Query()).Returns(comments.Object);

            // Act
            var result = await _commentService.DeleteCommentAsync(commentId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Forbid);
        }

        [Fact]
        public async Task DeleteCommentAsync_IsAuthor_DeletesComment()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var comment = new Comment 
            { 
                Id = commentId, 
                UserId = userId, // current user is comment author
                Post = new Post { UserId = "user-3" },
                IsDeleted = false
            };
            var comments = new List<Comment> { comment }.BuildMockDbSet();
            _mockCommentRepo.Setup(x => x.Query()).Returns(comments.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _commentService.DeleteCommentAsync(commentId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            comment.IsDeleted.Should().BeTrue();
            _mockCommentRepo.Verify(x => x.Update(It.IsAny<Comment>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_IsPostOwner_DeletesComment()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var userId = "user-1";
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
            
            var comment = new Comment 
            { 
                Id = commentId, 
                UserId = "user-2", // some other user wrote the comment
                Post = new Post { UserId = userId }, // but current user owns the post
                IsDeleted = false
            };
            var comments = new List<Comment> { comment }.BuildMockDbSet();
            _mockCommentRepo.Setup(x => x.Query()).Returns(comments.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _commentService.DeleteCommentAsync(commentId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            comment.IsDeleted.Should().BeTrue();
            _mockCommentRepo.Verify(x => x.Update(It.IsAny<Comment>()), Times.Once);
        }
    }
}
