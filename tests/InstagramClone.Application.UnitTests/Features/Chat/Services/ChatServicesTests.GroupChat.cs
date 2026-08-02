using FluentAssertions;
using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Application.UnitTests.Features.Chat.Services;

public partial class ChatServicesTests
{
    [Fact]
    public async Task AddMemberToGroupAsync_ShouldReturnSuccess_WhenUserHasPermissionAndTargetIsNotMember()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var targetUser = new AppUser(targetUserId, "target", "test@test.com", "First", "Last", "First Last");
        targetUser.SetAccountPrivacy(false);

        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        _mockChatRepository.Setup(r => r.IsParticipantAsync(chatRoomId, currentUserId)).ReturnsAsync(true);
        _mockChatRepository.Setup(r => r.IsParticipantAsync(chatRoomId, targetUserId)).ReturnsAsync(false);
        _mockChatRepository.Setup(r => r.GetRoomByIdAsync(chatRoomId)).ReturnsAsync(new ChatRoom(true, "Test Room"));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.AddMemberToGroupAsync(targetUserId.ToString(), chatRoomId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockChatRepository.Verify(r => r.AddParticipant(It.Is<ChatParticipant>(p => p.UserId == targetUserId && p.ChatRoomId == chatRoomId)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync($"chat:inbox:{targetUserId}"), Times.Once);
        _mockChatNotificationService.Verify(n => n.NotifyNewChatRoomAsync(It.IsAny<List<string>>(), chatRoomId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AddMemberToGroupAsync_ShouldReturnFailure_WhenCurrentUserIsNotMember()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var targetUser = new AppUser(targetUserId, "target", "test@test.com", "First", "Last", "First Last");
        _mockUserRepository.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);
        
        // Not a member
        _mockChatRepository.Setup(r => r.IsParticipantAsync(chatRoomId, currentUserId)).ReturnsAsync(false);

        // Act
        var result = await _chatServices.AddMemberToGroupAsync(targetUserId.ToString(), chatRoomId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Description.Should().Be("You are not a member of this group");
    }

    [Fact]
    public async Task RemoveMemberFromGroupAsync_ShouldReturnSuccess_WhenUserIsAdmin()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        _mockChatRepository.Setup(r => r.IsRoomAdminAsync(chatRoomId, currentUserId)).ReturnsAsync(true);
        var targetParticipant = new ChatParticipant(chatRoomId, targetUserId);
        _mockChatRepository.Setup(r => r.GetParticipantAsync(chatRoomId, targetUserId)).ReturnsAsync(targetParticipant);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.RemoveMemberFromGroupAsync(targetUserId.ToString(), chatRoomId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockChatRepository.Verify(r => r.RemoveParticipant(targetParticipant), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync($"chat:inbox:{targetUserId}"), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberFromGroupAsync_ShouldReturnFailure_WhenUserIsNotAdmin()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        _mockChatRepository.Setup(r => r.IsRoomAdminAsync(chatRoomId, currentUserId)).ReturnsAsync(false);

        // Act
        var result = await _chatServices.RemoveMemberFromGroupAsync(targetUserId.ToString(), chatRoomId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Description.Should().Contain("not an admin");
    }

    [Fact]
    public async Task CreateGroupRoomAsync_ShouldReturnSuccess_AndAddParticipants()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());
        _mockUserRepository.Setup(r => r.GetByIdAsync(currentUserId)).ReturnsAsync(new AppUser(currentUserId, "testuser", "test@test.com", "T", "U", "T U"));

        var dto = new CreateGroupDto
        {
            GroupName = "My Group",
            MemberIds = new List<string> { member1.ToString(), member2.ToString() }
        };

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.CreateGroupRoomAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        _mockChatRepository.Verify(r => r.AddRoom(It.Is<ChatRoom>(c => c.IsGroupChat == true && c.Name == "My Group")), Times.Once);
        _mockChatRepository.Verify(r => r.AddParticipants(It.Is<IEnumerable<ChatParticipant>>(p => true)), Times.Once); // Should have 3 participants
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockChatNotificationService.Verify(n => n.NotifyNewChatRoomAsync(dto.MemberIds, It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
    }
}
