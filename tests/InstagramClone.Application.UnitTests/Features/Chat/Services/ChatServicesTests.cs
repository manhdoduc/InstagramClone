using FluentAssertions;
using InstagramClone.Application.Features.Chat.Services;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Chats;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Models.Config;
using Microsoft.Extensions.Options;
using Moq;
using System;

namespace InstagramClone.Application.UnitTests.Features.Chat.Services;

public partial class ChatServicesTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IChatRepository> _mockChatRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IChatNotificationService> _mockChatNotificationService;
    private readonly Mock<IStorageServices> _mockStorageServices;
    private readonly Mock<IOptions<MediaSettings>> _mockMediaSettingsOptions;

    private readonly ChatServices _chatServices;

    public ChatServicesTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockChatRepository = new Mock<IChatRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockChatNotificationService = new Mock<IChatNotificationService>();
        _mockStorageServices = new Mock<IStorageServices>();
        _mockMediaSettingsOptions = new Mock<IOptions<MediaSettings>>();

        var mediaSettings = new MediaSettings
        {
            ChatImage = new ImageDimensionSettings { MaxWidth = 1080, MaxHeight = 1080 }
        };
        _mockMediaSettingsOptions.Setup(m => m.Value).Returns(mediaSettings);

        _mockUnitOfWork.Setup(u => u.Chats).Returns(_mockChatRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _chatServices = new ChatServices(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockCacheService.Object,
            _mockChatNotificationService.Object,
            _mockStorageServices.Object,
            _mockMediaSettingsOptions.Object
        );
    }
}
