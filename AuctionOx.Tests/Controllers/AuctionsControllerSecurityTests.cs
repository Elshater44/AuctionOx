using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.DTOs.Auctions;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Controllers
{
    public class AuctionsControllerSecurityTests
    {
        private readonly Mock<IAuctionService> _mockAuctionService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly AuctionsController _controller;

        public AuctionsControllerSecurityTests()
        {
            _mockAuctionService = new Mock<IAuctionService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _controller = new AuctionsController(_mockAuctionService.Object, _mockUserManager.Object);
        }

        private void SetControllerUser(string? userId)
        {
            var claims = new List<Claim>();
            if (userId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region BuyItNow - User Status Tests

        [Fact]
        public async Task BuyItNow_WhenUserIsActive_ExecutesPurchaseAndReturnsOk()
        {
            // Arrange
            const string userId = "active-buyer-1";
            const int auctionId = 5;
            var activeUser = new ApplicationUser
            {
                Id = userId,
                Email = "buyer@auctionox.com",
                IsDeleted = false,
                LockoutEnd = null
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(activeUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(activeUser)).ReturnsAsync(false);
            _mockAuctionService.Setup(s => s.BuyItNowAsync(auctionId, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(auctionId, userId), Times.Once);
        }

        [Fact]
        public async Task BuyItNow_WhenUserIsSuspended_ReturnsUnauthorized_AndDoesNotExecutePurchase()
        {
            // Arrange
            const string userId = "suspended-buyer-1";
            const int auctionId = 5;
            var suspendedUser = new ApplicationUser
            {
                Id = userId,
                Email = "suspended-buyer@auctionox.com",
                IsDeleted = false,
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(suspendedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(suspendedUser)).ReturnsAsync(true);

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task BuyItNow_WhenUserIsBanned_ReturnsUnauthorized_AndDoesNotExecutePurchase()
        {
            // Arrange
            const string userId = "banned-buyer-1";
            const int auctionId = 5;
            var bannedUser = new ApplicationUser
            {
                Id = userId,
                Email = "banned-buyer@auctionox.com",
                IsDeleted = false,
                LockoutEnd = DateTimeOffset.UtcNow.AddYears(100)
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(bannedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(bannedUser)).ReturnsAsync(true);

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task BuyItNow_WhenUserIsSoftDeleted_ReturnsUnauthorized_AndDoesNotExecutePurchase()
        {
            // Arrange
            const string userId = "deleted-buyer-1";
            const int auctionId = 5;
            var deletedUser = new ApplicationUser
            {
                Id = userId,
                Email = "deleted-buyer@auctionox.com",
                IsDeleted = true
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(deletedUser);

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task BuyItNow_WhenUserDoesNotExistInDatabase_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "nonexistent-buyer";
            const int auctionId = 5;

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task BuyItNow_WhenUserHasNoIdentifierClaim_ReturnsUnauthorized()
        {
            // Arrange
            const int auctionId = 5;

            SetControllerUser(null); // No claim

            // Act
            var result = await _controller.BuyItNow(auctionId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.BuyItNowAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region CreateAuction - User Status Tests

        [Fact]
        public async Task CreateAuction_WhenUserIsSuspended_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "suspended-creator";
            var request = new CreateAuctionRequest
            {
                Title = "Test Item",
                Description = "A valid description for testing",
                StartingPrice = 50m,
                CategoryId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var suspendedUser = new ApplicationUser
            {
                Id = userId,
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(suspendedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(suspendedUser)).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateAuction(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.CreateAuctionAsync(It.IsAny<string>(), It.IsAny<CreateAuctionRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateAuction_WhenUserIsSoftDeleted_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "deleted-creator";
            var request = new CreateAuctionRequest
            {
                Title = "Test Item",
                Description = "A valid description for testing",
                StartingPrice = 50m,
                CategoryId = 1,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            var deletedUser = new ApplicationUser
            {
                Id = userId,
                IsDeleted = true
            };

            SetControllerUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(deletedUser);

            // Act
            var result = await _controller.CreateAuction(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockAuctionService.Verify(s => s.CreateAuctionAsync(It.IsAny<string>(), It.IsAny<CreateAuctionRequest>()), Times.Never);
        }

        #endregion
    }
}
