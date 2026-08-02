using FluentAssertions;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Application.UnitTests.Features.Chat.Services;

public partial class ChatServicesTests
{
    [Fact]
    public async Task GetOrCreatePrivateRoomAsync_ShouldReturnExistingRoom_WhenItExists()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var existingRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var existingRoom = new ChatRoom(false, "");
        existingRoomId = existingRoom.Id;
        
        _mockChatRepository.Setup(r => r.GetPrivateRoomAsync(currentUserId, targetUserId)).ReturnsAsync(existingRoom);

        // Act
        var result = await _chatServices.GetOrCreatePrivateRoomAsync(targetUserId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingRoomId);
        
        _mockChatRepository.Verify(r => r.AddRoom(It.IsAny<ChatRoom>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetOrCreatePrivateRoomAsync_ShouldCreateNewRoom_WhenItDoesNotExist()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());
        
        var currentUser = new AppUser(currentUserId, "currentUser", "test@test.com", "F", "L", "F L");
        _mockUserRepository.Setup(r => r.GetByIdAsync(currentUserId)).ReturnsAsync(currentUser);

        _mockChatRepository.Setup(r => r.GetPrivateRoomAsync(currentUserId, targetUserId)).ReturnsAsync((ChatRoom?)null);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.GetOrCreatePrivateRoomAsync(targetUserId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockChatRepository.Verify(r => r.AddRoom(It.Is<ChatRoom>(c => c.IsGroupChat == false)), Times.Once);
        _mockChatRepository.Verify(r => r.AddParticipants(It.IsAny<System.Collections.Generic.IEnumerable<ChatParticipant>>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockChatNotificationService.Verify(n => n.NotifyNewChatRoomPrivateAsync(targetUserId.ToString(), It.IsAny<Guid>(), "currentUser"), Times.Once);
    }

    [Fact]
    public async Task GetOrCreatePrivateRoomAsync_ShouldReturnFailure_WhenTargetIsSelf()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        // Act
        var result = await _chatServices.GetOrCreatePrivateRoomAsync(currentUserId.ToString());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Failure);
    }
}
