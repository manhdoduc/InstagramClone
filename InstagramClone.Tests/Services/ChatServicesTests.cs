using FluentAssertions;
using InstagramClone.Application.DTOs.ChatDto;
using InstagramClone.Application.DTOs.Chats;
using InstagramClone.Application.Interfaces;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Application.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using AutoMapper;
using System.Linq.Expressions;

namespace InstagramClone.Tests.Services
{
    public class ChatServicesTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IChatNotificationService> _mockChatNotificationService;
        private readonly Mock<IStorageServices> _mockStorageServices;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ChatServices _chatServices;

        private readonly Mock<IGenericRepository<AppUser>> _mockUserRepo;
        private readonly Mock<IGenericRepository<ChatRoom>> _mockChatRoomRepo;
        private readonly Mock<IGenericRepository<ChatParticipant>> _mockChatParticipantRepo;
        private readonly Mock<IGenericRepository<Message>> _mockMessageRepo;

        public ChatServicesTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockChatNotificationService = new Mock<IChatNotificationService>();
            _mockStorageServices = new Mock<IStorageServices>();
            _mockMapper = new Mock<IMapper>();

            _mockUserRepo = new Mock<IGenericRepository<AppUser>>();
            _mockChatRoomRepo = new Mock<IGenericRepository<ChatRoom>>();
            _mockChatParticipantRepo = new Mock<IGenericRepository<ChatParticipant>>();
            _mockMessageRepo = new Mock<IGenericRepository<Message>>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.ChatRooms).Returns(_mockChatRoomRepo.Object);
            _mockUnitOfWork.Setup(u => u.ChatParticipants).Returns(_mockChatParticipantRepo.Object);
            _mockUnitOfWork.Setup(u => u.Messages).Returns(_mockMessageRepo.Object);

            _chatServices = new ChatServices(
                _mockUnitOfWork.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object,
                _mockChatNotificationService.Object,
                _mockStorageServices.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task AddMemberToGroupAsync_UserIsPrivate_ReturnsFailure()
        {
            // Arrange
            var currentUserId = "user-1";
            var targetUserId = "user-2";
            var roomId = Guid.NewGuid();
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
            
            var users = new List<AppUser> { new AppUser { Id = targetUserId, IsPrivateAccount = true } }.BuildMockDbSet();
            _mockUserRepo.Setup(x => x.QueryNoTracking()).Returns(users.Object);

            // Act
            var result = await _chatServices.AddMemberToGroupAsync(targetUserId, roomId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Failure);
        }

        [Fact]
        public async Task LeaveGroupAsync_ParticipantNotFound_ReturnsFailure()
        {
            // Arrange
            var currentUserId = "user-1";
            var roomId = Guid.NewGuid();
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
            
            var participants = new List<ChatParticipant>().BuildMockDbSet();
            _mockChatParticipantRepo.Setup(x => x.Query()).Returns(participants.Object);

            // Act
            var result = await _chatServices.LeaveGroupAsync(roomId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Failure);
        }

        [Fact]
        public async Task CreateMessageAsync_UserNotInRoom_ReturnsForbid()
        {
            // Arrange
            var currentUserId = "user-1";
            var roomId = Guid.NewGuid();
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
            
            var participants = new List<ChatParticipant>().BuildMockDbSet();
            _mockChatParticipantRepo.Setup(x => x.Query()).Returns(participants.Object);

            var request = new SendMessageDto { ChatRoomId = roomId, Content = "Hello" };

            // Act
            var result = await _chatServices.CreateMessageAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Forbid);
        }
    }
}
