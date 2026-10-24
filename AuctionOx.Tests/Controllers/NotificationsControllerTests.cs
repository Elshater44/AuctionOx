using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.DTOs.Common;
using AuctionOx.DTOs.Notifications;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly NotificationsController _controller;
        private const string TestUserId = "user-abc-123";

        public NotificationsControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _controller = new NotificationsController(_mockNotificationService.Object, _mockUserManager.Object);
        }

        private void SetAuthenticatedUser(string? userId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims, !string.IsNullOrEmpty(userId) ? "TestAuth" : null);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region Authorization and Attribute Tests

        [Fact]
        public void NotificationsController_HasExpectedAttributes()
        {
            var type = typeof(NotificationsController);

            type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
            var route = type.GetCustomAttribute<RouteAttribute>();
            route.Should().NotBeNull();
            route!.Template.Should().Be("api/[controller]");

            var auth = type.GetCustomAttribute<AuthorizeAttribute>();
            auth.Should().NotBeNull("NotificationsController must be guarded with [Authorize]");
        }

        #endregion

        #region GetNotifications Tests

        [Fact]
        public async Task GetNotifications_WithoutUserClaim_ReturnsUnauthorized()
        {
            SetAuthenticatedUser(null);

            var result = await _controller.GetNotifications();

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetNotifications_WithValidUserClaim_PassesCallerUserIdToService()
        {
            SetAuthenticatedUser(TestUserId);

            var pagedResponse = new PagedResult<NotificationDto>
            {
                Items = new List<NotificationDto>
                {
                    new() { Id = 1, Title = "Outbid!", Message = "You were outbid.", UserId = TestUserId }
                },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };

            _mockNotificationService
                .Setup(s => s.GetUserNotificationsAsync(TestUserId, 1, 10, null))
                .ReturnsAsync(pagedResponse);

            var result = await _controller.GetNotifications(pageNumber: 1, pageSize: 10, unreadOnly: null);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(pagedResponse);

            _mockNotificationService.Verify(
                s => s.GetUserNotificationsAsync(TestUserId, 1, 10, null),
                Times.Once,
                "Service must be invoked with the authenticated caller's userId");
        }

        #endregion

        #region GetUnreadCount Tests

        [Fact]
        public async Task GetUnreadCount_WithoutUserClaim_ReturnsUnauthorized()
        {
            SetAuthenticatedUser(null);

            var result = await _controller.GetUnreadCount();

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetUnreadCount_WithValidUserClaim_ReturnsCallerCount()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService.Setup(s => s.GetUnreadCountAsync(TestUserId)).ReturnsAsync(7);

            var result = await _controller.GetUnreadCount();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        #endregion

        #region MarkAsRead Tests

        [Fact]
        public async Task MarkAsRead_WithoutUserClaim_ReturnsUnauthorized()
        {
            SetAuthenticatedUser(null);

            var result = await _controller.MarkAsRead(1);

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task MarkAsRead_WhenNotificationNotFoundOrBelongsToAnotherUser_ReturnsNotFound()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService
                .Setup(s => s.MarkAsReadAsync(99, TestUserId))
                .ReturnsAsync(false);

            var result = await _controller.MarkAsRead(99);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task MarkAsRead_WhenOwnedByCaller_ReturnsOk()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService
                .Setup(s => s.MarkAsReadAsync(1, TestUserId))
                .ReturnsAsync(true);

            var result = await _controller.MarkAsRead(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region MarkAllAsRead Tests

        [Fact]
        public async Task MarkAllAsRead_WithoutUserClaim_ReturnsUnauthorized()
        {
            SetAuthenticatedUser(null);

            var result = await _controller.MarkAllAsRead();

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task MarkAllAsRead_WithValidUserClaim_ReturnsMarkedCount()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService
                .Setup(s => s.MarkAllAsReadAsync(TestUserId))
                .ReturnsAsync(4);

            var result = await _controller.MarkAllAsRead();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        #endregion

        #region DeleteNotification Tests

        [Fact]
        public async Task DeleteNotification_WithoutUserClaim_ReturnsUnauthorized()
        {
            SetAuthenticatedUser(null);

            var result = await _controller.DeleteNotification(1);

            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task DeleteNotification_WhenNotFoundOrNotCallerOwner_ReturnsNotFound()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService
                .Setup(s => s.DeleteNotificationAsync(55, TestUserId))
                .ReturnsAsync(false);

            var result = await _controller.DeleteNotification(55);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteNotification_WhenOwnedByCaller_ReturnsNoContent()
        {
            SetAuthenticatedUser(TestUserId);
            _mockNotificationService
                .Setup(s => s.DeleteNotificationAsync(1, TestUserId))
                .ReturnsAsync(true);

            var result = await _controller.DeleteNotification(1);

            result.Should().BeOfType<NoContentResult>();
        }

        #endregion
    }
}
