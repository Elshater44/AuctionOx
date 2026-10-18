using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;
using AuctionOx.Exceptions;
using AuctionOx.Models;
using AuctionOx.Services.Implementations;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly IConfiguration _config;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "JwtSettings:Secret", "SuperSecretDevKeyForAuctionOxProjectAtLeast32BytesLong!" },
                { "JwtSettings:Issuer", "AuctionOx" },
                { "JwtSettings:Audience", "AuctionOx" },
                { "JwtSettings:ExpiryMinutes", "60" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            var logger = NullLogger<AuthService>.Instance;
            _authService = new AuthService(_mockUserManager.Object, _config, logger);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_CreatesUser_AssignsRole_AndReturnsAuthResponse()
        {
            var request = new RegisterRequest
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                FirstName = "Jane",
                LastName = "Doe"
            };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });

            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.RegisterAsync(request);

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.Expiration.Should().BeAfter(DateTime.UtcNow);

            _mockUserManager.Verify(m => m.CreateAsync(It.Is<ApplicationUser>(u => u.Email == request.Email), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenCreationFails_ThrowsBusinessRuleException()
        {
            var request = new RegisterRequest
            {
                Email = "duplicate@example.com",
                Password = "Password123!",
                FirstName = "Duplicate",
                LastName = "User"
            };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email is already taken." }));

            var act = () => _authService.RegisterAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Email is already taken.*");
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
        {
            var request = new LoginRequest
            {
                Email = "validuser@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = request.Email,
                UserName = request.Email
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _mockUserManager.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.LoginAsync(request);

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.Expiration.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task LoginAsync_WhenUserNotFound_ThrowsBusinessRuleException()
        {
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            var act = () => _authService.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIncorrect_ThrowsBusinessRuleException()
        {
            var request = new LoginRequest
            {
                Email = "validuser@example.com",
                Password = "WrongPassword!"
            };

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = request.Email
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(false);

            var act = () => _authService.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task GetProfileAsync_WhenUserExists_ReturnsUserProfileDto()
        {
            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "user@example.com",
                FirstName = "Alice",
                LastName = "Smith",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            _mockUserManager.Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(user);

            var profile = await _authService.GetProfileAsync("user-1");

            profile.Should().NotBeNull();
            profile!.Id.Should().Be("user-1");
            profile.Email.Should().Be("user@example.com");
            profile.FirstName.Should().Be("Alice");
            profile.LastName.Should().Be("Smith");
        }

        [Fact]
        public async Task GetProfileAsync_WhenUserDoesNotExist_ReturnsNull()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("unknown-id"))
                .ReturnsAsync((ApplicationUser?)null);

            var profile = await _authService.GetProfileAsync("unknown-id");
            profile.Should().BeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidTokens_ReturnsRotatedTokens()
        {
            // Arrange: Generate an initial token
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "user@example.com"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.FindByIdAsync("user-123")).ReturnsAsync(user);

            var initialLogin = await _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" });
            var originalRefreshToken = initialLogin.RefreshToken;

            // Act: Refresh
            var refreshed = await _authService.RefreshTokenAsync(initialLogin.Token, initialLogin.RefreshToken);

            // Assert
            refreshed.Should().NotBeNull();
            refreshed.Token.Should().NotBeNullOrEmpty();
            refreshed.RefreshToken.Should().NotBeNullOrEmpty();
            refreshed.RefreshToken.Should().NotBe(originalRefreshToken); // Rotation confirmed!
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenDoesNotMatch_ThrowsBusinessRuleException()
        {
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "user@example.com"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.FindByIdAsync("user-123")).ReturnsAsync(user);

            var initialLogin = await _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" });

            // Act with corrupted refresh token
            var act = () => _authService.RefreshTokenAsync(initialLogin.Token, "wrong-refresh-token");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Invalid refresh token");
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenExpired_ThrowsBusinessRuleException()
        {
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "user@example.com"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.FindByIdAsync("user-123")).ReturnsAsync(user);

            var initialLogin = await _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" });

            // Expire token
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-10);

            var act = () => _authService.RefreshTokenAsync(initialLogin.Token, initialLogin.RefreshToken);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Invalid refresh token");
        }
    }
}
