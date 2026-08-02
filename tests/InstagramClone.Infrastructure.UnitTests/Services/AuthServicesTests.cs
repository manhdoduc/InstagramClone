using FluentAssertions;
using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Domain.Entities;
using InstagramClone.Infrastructure.Identity;
using InstagramClone.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InstagramClone.Infrastructure.UnitTests.Services;

public partial class AuthServicesTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtOptions;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    
    private readonly AuthServices _authServices;

    public AuthServicesTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _mockJwtOptions = new Mock<IOptions<JwtSettings>>();
        var jwtSettings = new JwtSettings
        {
            Key = "a_very_long_secret_key_that_is_at_least_32_bytes_long", // Needs at least 32 bytes for HMAC-SHA256
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryInMinutes = 60,
            RefreshTokenExpiryTime = 7
        };
        _mockJwtOptions.Setup(o => o.Value).Returns(jwtSettings);

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _authServices = new AuthServices(
            _mockUserManager.Object,
            _mockJwtOptions.Object,
            _mockCurrentUserService.Object,
            _mockUnitOfWork.Object
        );
    }
}
