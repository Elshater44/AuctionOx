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

        #region CreateAdminAsync & GetAdminsAsync Tests

        [Fact]
        public async Task CreateAdminAsync_WithValidData_CreatesUser_AssignsAdminRole_AndReturnsAdminUserDto()
        {
            var request = new CreateAdminRequest
            {
                Email = "newadmin@auctionox.com",
                Password = "AdminSecurePassword123!",
                FirstName = "Super",
                LastName = "Admin"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.CreateAdminAsync(request);

            result.Should().NotBeNull();
            result.Email.Should().Be(request.Email);
            result.FirstName.Should().Be(request.FirstName);
            result.LastName.Should().Be(request.LastName);
            result.Roles.Should().Contain("Admin");

            _mockUserManager.Verify(m => m.CreateAsync(It.Is<ApplicationUser>(u => u.Email == request.Email && u.EmailConfirmed), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Email == request.Email), "Admin"), Times.Once);
        }

        [Fact]
        public async Task CreateAdminAsync_WhenEmailAlreadyExists_ThrowsBusinessRuleException()
        {
            var request = new CreateAdminRequest
            {
                Email = "existing@auctionox.com",
                Password = "AdminSecurePassword123!",
                FirstName = "Super",
                LastName = "Admin"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(new ApplicationUser { Email = request.Email });

            var act = () => _authService.CreateAdminAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("A user with this email already exists.");

            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAdminAsync_WhenIdentityCreationFails_ThrowsBusinessRuleException()
        {
            var request = new CreateAdminRequest
            {
                Email = "newadmin@auctionox.com",
                Password = "short",
                FirstName = "Super",
                LastName = "Admin"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too short" }));

            var act = () => _authService.CreateAdminAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Password too short*");

            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAdminAsync_WhenRoleAssignmentFails_ThrowsBusinessRuleException()
        {
            var request = new CreateAdminRequest
            {
                Email = "newadmin@auctionox.com",
                Password = "AdminSecurePassword123!",
                FirstName = "Super",
                LastName = "Admin"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));

            var act = () => _authService.CreateAdminAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Failed to assign Administrator role.");
        }

        [Fact]
        public async Task GetAdminsAsync_ReturnsOnlyActiveAdminUsers()
        {
            var adminList = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "admin-1", Email = "admin1@auctionox.com", FirstName = "Admin", LastName = "One", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new ApplicationUser { Id = "admin-2", Email = "admin2@auctionox.com", FirstName = "Admin", LastName = "Two", IsDeleted = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new ApplicationUser { Id = "admin-3", Email = "admin3@auctionox.com", FirstName = "Admin", LastName = "Three", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(adminList);

            var result = (await _authService.GetAdminsAsync()).ToList();

            result.Should().HaveCount(2);
            result.Select(a => a.Email).Should().Contain(new[] { "admin1@auctionox.com", "admin3@auctionox.com" });
            result.Select(a => a.Email).Should().NotContain("admin2@auctionox.com");
            result.First().Email.Should().Be("admin3@auctionox.com"); // sorted descending by CreatedAt
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenDemotingToUser_RemovesAdminRole_AddsUserRole_AndInvalidatesTokens()
        {
            var targetUser = new ApplicationUser
            {
                Id = "target-admin-id",
                Email = "otheradmin@auctionox.com",
                FirstName = "Other",
                LastName = "Admin",
                RefreshToken = "valid-refresh-token"
            };

            var adminList = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "admin-1", Email = "admin@auctionox.com" },
                targetUser
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "Admin" });
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(adminList);
            _mockUserManager.Setup(m => m.RemoveFromRoleAsync(targetUser, "Admin")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(targetUser, "User")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _authService.UpdateAdminRoleAsync(targetUser.Id, "User", "current-admin-id");

            result.Should().NotBeNull();
            targetUser.RefreshToken.Should().BeNull();
            _mockUserManager.Verify(m => m.RemoveFromRoleAsync(targetUser, "Admin"), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(targetUser, "User"), Times.Once);
            _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenSelfDemotion_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser
            {
                Id = "admin-self-id",
                Email = "self@auctionox.com"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);

            var act = () => _authService.UpdateAdminRoleAsync(targetUser.Id, "User", "admin-self-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("You cannot modify your own administrator role.");
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenRootAdmin_ThrowsBusinessRuleException()
        {
            var rootAdmin = new ApplicationUser
            {
                Id = "root-admin-id",
                Email = "admin@auctionox.com"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(rootAdmin.Id)).ReturnsAsync(rootAdmin);

            var act = () => _authService.UpdateAdminRoleAsync(rootAdmin.Id, "User", "other-admin-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("The root system administrator account cannot be demoted or modified.");
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenOnlyOneAdminRemains_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser
            {
                Id = "only-admin-id",
                Email = "onlyadmin@auctionox.com"
            };

            var singleAdminList = new List<ApplicationUser> { targetUser };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser)).ReturnsAsync(new List<string> { "Admin" });
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(singleAdminList);

            var act = () => _authService.UpdateAdminRoleAsync(targetUser.Id, "User", "other-caller-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Cannot demote the only remaining administrator on the platform.");
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenPromotingToAdmin_AddsAdminRole_AndInvalidatesTokens()
        {
            var targetUser = new ApplicationUser
            {
                Id = "regular-user-id",
                Email = "regular@auctionox.com",
                FirstName = "Jane",
                LastName = "User",
                RefreshToken = "valid-token"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.AddToRoleAsync(targetUser, "Admin")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _authService.UpdateAdminRoleAsync(targetUser.Id, "Admin", "current-admin-id");

            result.Should().NotBeNull();
            targetUser.RefreshToken.Should().BeNull();
            _mockUserManager.Verify(m => m.AddToRoleAsync(targetUser, "Admin"), Times.Once);
            _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task UpdateAdminRoleAsync_WhenPromotingSuspendedUser_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser
            {
                Id = "suspended-user-id",
                Email = "suspended@auctionox.com",
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser))
                .ReturnsAsync(new List<string> { "User" });

            var act = () => _authService.UpdateAdminRoleAsync(targetUser.Id, "Admin", "current-admin-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Cannot grant administrator privileges to a suspended or banned user.");
        }

        [Fact]
        public async Task DeleteAdminAsync_WithValidAdmin_DeletesAccount_AndInvalidatesTokens()
        {
            var targetUser = new ApplicationUser
            {
                Id = "target-admin-id",
                Email = "demoteme@auctionox.com",
                RefreshToken = "active-token"
            };

            var adminList = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "admin-1", Email = "admin@auctionox.com" },
                targetUser
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(adminList);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.DeleteAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            await _authService.DeleteAdminAsync(targetUser.Id, "current-admin-id");

            targetUser.IsDeleted.Should().BeTrue();
            targetUser.RefreshToken.Should().BeNull();
            _mockUserManager.Verify(m => m.DeleteAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task DeleteAdminAsync_WhenSelfDelete_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser
            {
                Id = "admin-self-id",
                Email = "self@auctionox.com"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);

            var act = () => _authService.DeleteAdminAsync(targetUser.Id, "admin-self-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("You cannot delete your own administrator account.");
        }

        [Fact]
        public async Task DeleteAdminAsync_WhenRootAdmin_ThrowsBusinessRuleException()
        {
            var rootAdmin = new ApplicationUser
            {
                Id = "root-admin-id",
                Email = "admin@auctionox.com"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(rootAdmin.Id)).ReturnsAsync(rootAdmin);

            var act = () => _authService.DeleteAdminAsync(rootAdmin.Id, "other-admin-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("The root system administrator account cannot be deleted.");
        }

        [Fact]
        public async Task DeleteAdminAsync_WhenOnlyOneAdminRemains_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser
            {
                Id = "last-admin-id",
                Email = "lastadmin@auctionox.com"
            };

            var singleAdminList = new List<ApplicationUser> { targetUser };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(singleAdminList);

            var act = () => _authService.DeleteAdminAsync(targetUser.Id, "other-caller-id");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Cannot delete the only remaining administrator on the platform.");
        }

        [Fact]
        public async Task LoginAsync_WhenUserIsLockedOut_ThrowsBusinessRuleException()
        {
            var user = new ApplicationUser
            {
                Id = "locked-user-id",
                Email = "locked@auctionox.com",
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(5)
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var act = () => _authService.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" });

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*suspended*");
        }

        [Fact]
        public async Task SuspendUserAsync_WithValidUser_SetsLockoutEndDate_AndInvalidatesTokens()
        {
            var targetUser = new ApplicationUser
            {
                Id = "target-user-id",
                Email = "badactor@auctionox.com",
                RefreshToken = "active-refresh-token"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.SetLockoutEnabledAsync(targetUser, true)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(targetUser, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _authService.SuspendUserAsync(targetUser.Id, 14, "admin-caller-id", "Rule violations");

            result.Should().NotBeNull();
            result.IsLockedOut.Should().BeTrue();
            result.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(13));
            targetUser.RefreshToken.Should().BeNull();

            _mockUserManager.Verify(m => m.SetLockoutEnabledAsync(targetUser, true), Times.Once);
            _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(targetUser, It.Is<DateTimeOffset>(d => d > DateTimeOffset.UtcNow)), Times.Once);
            _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task SuspendUserAsync_WhenSelfSuspension_ThrowsBusinessRuleException()
        {
            var targetUser = new ApplicationUser { Id = "admin-self", Email = "admin@example.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);

            var act = () => _authService.SuspendUserAsync(targetUser.Id, 7, "admin-self");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("You cannot suspend your own account.");
        }

        [Fact]
        public async Task SuspendUserAsync_WhenRootAdmin_ThrowsBusinessRuleException()
        {
            var rootAdmin = new ApplicationUser { Id = "root-id", Email = "admin@auctionox.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync(rootAdmin.Id)).ReturnsAsync(rootAdmin);

            var act = () => _authService.SuspendUserAsync(rootAdmin.Id, 7, "other-admin");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("The root system administrator account cannot be suspended.");
        }

        [Fact]
        public async Task BanUserAsync_SetsPermanentLockout_AndInvalidatesTokens()
        {
            var targetUser = new ApplicationUser
            {
                Id = "spammer-id",
                Email = "spammer@auctionox.com",
                RefreshToken = "some-token"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.SetLockoutEnabledAsync(targetUser, true)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(targetUser, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _authService.BanUserAsync(targetUser.Id, "admin-caller-id", "Fraudulent activities");

            result.Should().NotBeNull();
            result.IsLockedOut.Should().BeTrue();
            result.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow.AddYears(50));
            targetUser.RefreshToken.Should().BeNull();

            _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(targetUser, It.Is<DateTimeOffset>(d => d > DateTimeOffset.UtcNow.AddYears(50))), Times.Once);
        }

        [Fact]
        public async Task ReactivateUserAsync_ClearsLockoutEndDate_AndResetsFailedCount()
        {
            var targetUser = new ApplicationUser
            {
                Id = "reformed-user-id",
                Email = "reformed@auctionox.com",
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(10)
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
            _mockUserManager.Setup(m => m.GetRolesAsync(targetUser)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(targetUser, null)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.ResetAccessFailedCountAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(targetUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _authService.ReactivateUserAsync(targetUser.Id, "admin-caller-id");

            result.Should().NotBeNull();
            result.IsLockedOut.Should().BeFalse();
            result.LockoutEnd.Should().BeNull();

            _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(targetUser, null), Times.Once);
            _mockUserManager.Verify(m => m.ResetAccessFailedCountAsync(targetUser), Times.Once);
        }

        #endregion
    }
}
