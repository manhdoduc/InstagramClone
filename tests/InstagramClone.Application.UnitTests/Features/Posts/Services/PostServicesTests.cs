using FluentAssertions;
using InstagramClone.Application.Common;
using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.Services;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Posts.Services;
using InstagramClone.Application.Interfaces.Caching;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace InstagramClone.Application.UnitTests.Features.Posts.Services;

public partial class PostServicesTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IStorageServices> _mockStorageServices;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<IOptions<MediaSettings>> _mockMediaSettingsOptions;
    
    private readonly PostServices _postServices;

    public PostServicesTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockStorageServices = new Mock<IStorageServices>();
        _mockCacheService = new Mock<ICacheService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockMediaSettingsOptions = new Mock<IOptions<MediaSettings>>();

        var mediaSettings = new MediaSettings
        {
            Post = new ImageDimensionSettings { MaxWidth = 1080, MaxHeight = 1080 }
        };
        _mockMediaSettingsOptions.Setup(x => x.Value).Returns(mediaSettings);
        
        _mockUnitOfWork.Setup(u => u.Posts).Returns(_mockPostRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _postServices = new PostServices(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockStorageServices.Object,
            _mockCacheService.Object,
            _mockBackgroundJobService.Object,
            _mockMediaSettingsOptions.Object
        );
    }
}
