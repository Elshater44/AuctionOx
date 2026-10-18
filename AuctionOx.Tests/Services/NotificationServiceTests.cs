using System;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Notifications;
using AuctionOx.Hubs;
using AuctionOx.Hubs.Clients;
using AuctionOx.Mappings;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Services.Implementations;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext;
        private readonly Mock<IHubClients<INotificationClient>> _mockClients;
        private readonly Mock<INotificationClient> _mockSingleClient;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<MappingProfile>();
            var config = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(dbOptions);

            var unitOfWork = new UnitOfWork(_dbContext);
            var logger = NullLogger<NotificationService>.Instance;

            _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
            _mockClients = new Mock<IHubClients<INotificationClient>>();
            _mockSingleClient = new Mock<INotificationClient>();

            _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockSingleClient.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

            _notificationService = new NotificationService(unitOfWork, _mapper, logger, _mockHubContext.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task CreateAndSendNotificationAsync_PersistsNotification_AndSendsToSignalR()
        {
            // Arrange
            var userId = "user-123";

            // Act
            var result = await _notificationService.CreateAndSendNotificationAsync(
                userId,
                "Outbid Alert!",
                "You have been outbid on Laptop.",
                NotificationType.Outbid,
                10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Outbid Alert!", result.Title);
            Assert.Equal(NotificationType.Outbid, result.Type);
            Assert.False(result.IsRead);

            var inDb = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == result.Id);
            Assert.NotNull(inDb);
            Assert.Equal(userId, inDb.UserId);

            _mockSingleClient.Verify(c => c.ReceiveNotification(It.Is<NotificationDto>(dto => dto.Id == result.Id)), Times.Once);
            _mockSingleClient.Verify(c => c.UpdateUnreadCount(1), Times.Once);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ReturnsOnlyUserNotifications_WithPagination()
        {
            // Arrange
            var user1 = "user-1";
            var user2 = "user-2";

            await _notificationService.CreateAndSendNotificationAsync(user1, "Alert 1", "Msg 1", NotificationType.NewBid);
            await _notificationService.CreateAndSendNotificationAsync(user1, "Alert 2", "Msg 2", NotificationType.Outbid);
            await _notificationService.CreateAndSendNotificationAsync(user2, "Alert 3", "Msg 3", NotificationType.AuctionWon);

            // Act
            var result = await _notificationService.GetUserNotificationsAsync(user1, pageNumber: 1, pageSize: 10);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.Equal(user1, item.UserId));
        }

        [Fact]
        public async Task MarkAsReadAsync_UpdatesStatus_AndDispatchesNewCount()
        {
            // Arrange
            var userId = "user-1";
            var created = await _notificationService.CreateAndSendNotificationAsync(userId, "Alert", "Msg", NotificationType.NewBid);

            // Act
            var success = await _notificationService.MarkAsReadAsync(created.Id, userId);

            // Assert
            Assert.True(success);
            var updated = await _dbContext.Notifications.FindAsync(created.Id);
            Assert.NotNull(updated);
            Assert.True(updated.IsRead);

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            Assert.Equal(0, unreadCount);
        }

        [Fact]
        public async Task MarkAsReadAsync_WrongUser_ReturnsFalse()
        {
            // Arrange
            var userId = "user-1";
            var otherUser = "user-2";
            var created = await _notificationService.CreateAndSendNotificationAsync(userId, "Alert", "Msg", NotificationType.NewBid);

            // Act
            var success = await _notificationService.MarkAsReadAsync(created.Id, otherUser);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllForUser()
        {
            // Arrange
            var userId = "user-1";
            await _notificationService.CreateAndSendNotificationAsync(userId, "Alert 1", "Msg 1", NotificationType.NewBid);
            await _notificationService.CreateAndSendNotificationAsync(userId, "Alert 2", "Msg 2", NotificationType.Outbid);

            // Act
            var count = await _notificationService.MarkAllAsReadAsync(userId);

            // Assert
            Assert.Equal(2, count);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            Assert.Equal(0, unreadCount);
            _mockSingleClient.Verify(c => c.UpdateUnreadCount(0), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteNotificationAsync_SoftDeletesNotification()
        {
            // Arrange
            var userId = "user-1";
            var created = await _notificationService.CreateAndSendNotificationAsync(userId, "Alert 1", "Msg 1", NotificationType.NewBid);

            // Act
            var deleted = await _notificationService.DeleteNotificationAsync(created.Id, userId);

            // Assert
            Assert.True(deleted);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            Assert.Equal(0, unreadCount);
        }
    }
}
