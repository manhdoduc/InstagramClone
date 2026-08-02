using FluentAssertions;
using InstagramClone.Application.Features.Chat.DTOs;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Common;
using InstagramClone.Domain.Entities;
using InstagramClone.Domain.Enums;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Application.UnitTests.Features.Chat.Services;

public partial class ChatServicesTests
{
    [Fact]
    public async Task CreateMessageAsync_ShouldReturnSuccess_WhenUserIsParticipant()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());
        _mockCurrentUserService.Setup(c => c.UserName).Returns("testuser");

        var participant = new ChatParticipant(chatRoomId, currentUserId);
        _mockChatRepository.Setup(r => r.GetParticipantAsync(chatRoomId, currentUserId)).ReturnsAsync(participant);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var request = new SendMessageDto
        {
            ChatRoomId = chatRoomId,
            Type = MessageType.Text,
            Content = "Hello World"
        };

        // Act
        var result = await _chatServices.CreateMessageAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Content.Should().Be("Hello World");
        
        _mockChatRepository.Verify(r => r.AddMessage(It.IsAny<Message>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockChatNotificationService.Verify(n => n.NotifyReceiveMessageAsync(chatRoomId, It.IsAny<MessageDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_ShouldReturnForbid_WhenUserIsNotParticipant()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var chatRoomId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        _mockChatRepository.Setup(r => r.GetParticipantAsync(chatRoomId, currentUserId)).ReturnsAsync((ChatParticipant?)null);

        var request = new SendMessageDto { ChatRoomId = chatRoomId, Type = MessageType.Text, Content = "Hello World" };

        // Act
        var result = await _chatServices.CreateMessageAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbid);
    }

    [Fact]
    public async Task UnsendMessageAsync_ShouldReturnSuccess_WhenWithinTimeLimit()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var message = new Message(Guid.NewGuid(), currentUserId, MessageType.Text, "test", null, null);
        // Message is created now, so it's within the 24h limit
        
        _mockChatRepository.Setup(r => r.GetUserMessageByIdAsync(messageId, currentUserId)).ReturnsAsync(message);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.UnsendMessageAsync(messageId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.IsDeleted.Should().BeTrue(); // Unsend method sets this
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockChatNotificationService.Verify(n => n.NotifyMessageUnsentAsync(message.ChatRoomId, messageId), Times.Once);
    }

    [Fact]
    public async Task UnsendMessageAsync_ShouldReturnForbid_WhenTimeLimitExpired()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var message = new Message(Guid.NewGuid(), currentUserId, MessageType.Text, "test", null, null);
        
        // Use reflection to set CreatedAt to 2 days ago
        var property = typeof(BaseEntity).GetProperty("CreatedAt");
        property!.SetValue(message, DateTime.UtcNow.AddDays(-2));
        
        _mockChatRepository.Setup(r => r.GetUserMessageByIdAsync(messageId, currentUserId)).ReturnsAsync(message);

        // Act
        var result = await _chatServices.UnsendMessageAsync(messageId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbid);
        result.Errors[0].Description.Should().Contain("limit expired");
    }

    [Fact]
    public async Task ReactToMessageAsync_ShouldRemoveReaction_WhenEmojiIsSame()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), MessageType.Text, "test", null, null);
        var existingReaction = new MessageReaction(messageId, currentUserId, "❤️");

        _mockChatRepository.Setup(r => r.GetMessageByIdAsync(messageId)).ReturnsAsync(message);
        _mockChatRepository.Setup(r => r.GetReactionAsync(messageId, currentUserId)).ReturnsAsync(existingReaction);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.ReactToMessageAsync(messageId, "❤️");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockChatRepository.Verify(r => r.RemoveReaction(existingReaction), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReactToMessageAsync_ShouldChangeEmoji_WhenEmojiIsDifferent()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(currentUserId.ToString());

        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), MessageType.Text, "test", null, null);
        var existingReaction = new MessageReaction(messageId, currentUserId, "❤️");

        _mockChatRepository.Setup(r => r.GetMessageByIdAsync(messageId)).ReturnsAsync(message);
        _mockChatRepository.Setup(r => r.GetReactionAsync(messageId, currentUserId)).ReturnsAsync(existingReaction);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _chatServices.ReactToMessageAsync(messageId, "😂");

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingReaction.Emoji.Should().Be("😂");
        _mockChatRepository.Verify(r => r.RemoveReaction(It.IsAny<MessageReaction>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
