using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.DTOs.Bids;
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
    public class BidsControllerSecurityTests
    {
        private readonly Mock<IBidService> _mockBidService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly BidsController _bidsController;
        private readonly MyBidsController _myBidsController;

        public BidsControllerSecurityTests()
        {
            _mockBidService = new Mock<IBidService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _bidsController = new BidsController(_mockBidService.Object, _mockUserManager.Object);
            _myBidsController = new MyBidsController(_mockBidService.Object, _mockUserManager.Object);
        }

        private void SetControllerUser(ControllerBase controller, string? userId)
        {
            var claims = new List<Claim>();
            if (userId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region PlaceBid - User Status Tests

        [Fact]
        public async Task PlaceBid_WhenUserIsActive_PlacesBidAndReturnsCreated()
        {
            // Arrange
            const string userId = "active-user-1";
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };
            var activeUser = new ApplicationUser
            {
                Id = userId,
                Email = "active@auctionox.com",
                IsDeleted = false,
                LockoutEnd = null
            };

            SetControllerUser(_bidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(activeUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(activeUser)).ReturnsAsync(false);

            var expectedBidDto = new BidDto
            {
                Id = 1,
                AuctionItemId = auctionId,
                BidAmount = 150m,
                BidderId = userId,
                BidderName = "active@auctionox.com"
            };

            _mockBidService.Setup(s => s.PlaceBidAsync(auctionId, userId, request))
                .ReturnsAsync(expectedBidDto);

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.Value.Should().BeEquivalentTo(expectedBidDto);
            _mockBidService.Verify(s => s.PlaceBidAsync(auctionId, userId, request), Times.Once);
        }

        [Fact]
        public async Task PlaceBid_WhenUserIsSuspended_ReturnsUnauthorized_AndDoesNotPlaceBid()
        {
            // Arrange
            const string userId = "suspended-user-1";
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };
            var suspendedUser = new ApplicationUser
            {
                Id = userId,
                Email = "suspended@auctionox.com",
                IsDeleted = false,
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
            };

            SetControllerUser(_bidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(suspendedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(suspendedUser)).ReturnsAsync(true);

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockBidService.Verify(s => s.PlaceBidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PlaceBidRequest>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBid_WhenUserIsBanned_ReturnsUnauthorized_AndDoesNotPlaceBid()
        {
            // Arrange
            const string userId = "banned-user-1";
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };
            var bannedUser = new ApplicationUser
            {
                Id = userId,
                Email = "banned@auctionox.com",
                IsDeleted = false,
                LockoutEnd = DateTimeOffset.UtcNow.AddYears(100)
            };

            SetControllerUser(_bidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(bannedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(bannedUser)).ReturnsAsync(true);

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockBidService.Verify(s => s.PlaceBidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PlaceBidRequest>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBid_WhenUserIsSoftDeleted_ReturnsUnauthorized_AndDoesNotPlaceBid()
        {
            // Arrange
            const string userId = "deleted-user-1";
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };
            var deletedUser = new ApplicationUser
            {
                Id = userId,
                Email = "deleted@auctionox.com",
                IsDeleted = true
            };

            SetControllerUser(_bidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(deletedUser);

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockBidService.Verify(s => s.PlaceBidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PlaceBidRequest>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBid_WhenUserDoesNotExistInDatabase_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "ghost-user-1";
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };

            SetControllerUser(_bidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockBidService.Verify(s => s.PlaceBidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PlaceBidRequest>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBid_WhenUserHasNoIdentifierClaim_ReturnsUnauthorized()
        {
            // Arrange
            const int auctionId = 10;
            var request = new PlaceBidRequest { BidAmount = 150m };

            SetControllerUser(_bidsController, null); // No claim

            // Act
            var result = await _bidsController.PlaceBid(auctionId, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _mockBidService.Verify(s => s.PlaceBidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PlaceBidRequest>()), Times.Never);
        }

        #endregion

        #region MyBids - User Status Tests

        [Fact]
        public async Task GetMyBids_WhenUserIsActive_ReturnsOk()
        {
            // Arrange
            const string userId = "active-user-2";
            var activeUser = new ApplicationUser { Id = userId, IsDeleted = false };

            SetControllerUser(_myBidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(activeUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(activeUser)).ReturnsAsync(false);

            var expectedBids = new List<BidDto>
            {
                new BidDto { Id = 1, BidAmount = 100m, BidderId = userId }
            };

            _mockBidService.Setup(s => s.GetMyBidsAsync(userId)).ReturnsAsync(expectedBids);

            // Act
            var result = await _myBidsController.GetMyBids();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedBids);
        }

        [Fact]
        public async Task GetMyBids_WhenUserIsSuspended_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "suspended-user-2";
            var suspendedUser = new ApplicationUser
            {
                Id = userId,
                LockoutEnd = DateTimeOffset.UtcNow.AddDays(14)
            };

            SetControllerUser(_myBidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(suspendedUser);
            _mockUserManager.Setup(m => m.IsLockedOutAsync(suspendedUser)).ReturnsAsync(true);

            // Act
            var result = await _myBidsController.GetMyBids();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMyBids_WhenUserIsSoftDeleted_ReturnsUnauthorized()
        {
            // Arrange
            const string userId = "deleted-user-2";
            var deletedUser = new ApplicationUser { Id = userId, IsDeleted = true };

            SetControllerUser(_myBidsController, userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(deletedUser);

            // Act
            var result = await _myBidsController.GetMyBids();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        #endregion
    }
}
