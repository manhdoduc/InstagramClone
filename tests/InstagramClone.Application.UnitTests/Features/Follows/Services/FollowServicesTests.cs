using FluentAssertions;
using InstagramClone.Application.Features.Follows.Services;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using Moq;
using System;

namespace InstagramClone.Application.UnitTests.Features.Follows.Services;

public partial class FollowServicesTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;

    private readonly FollowServices _followServices;

    public FollowServicesTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _followServices = new FollowServices(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockCacheService.Object,
            _mockBackgroundJobService.Object
        );
    }
}
