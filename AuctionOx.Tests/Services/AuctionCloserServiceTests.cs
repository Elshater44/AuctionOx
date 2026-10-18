using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuctionOx.BackgroundServices;
using AuctionOx.Data;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Repositories.Interfaces;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class AuctionCloserServiceTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AuctionCloserService _closerService;

        public AuctionCloserServiceTests()
        {
            _databaseName = Guid.NewGuid().ToString();
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            _mockNotificationService = new Mock<INotificationService>();

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: _databaseName));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<INotificationService>(_ => _mockNotificationService.Object);
            _serviceProvider = services.BuildServiceProvider();

            var logger = NullLogger<AuctionCloserService>.Instance;
            _closerService = new AuctionCloserService(_serviceProvider, logger);
        }

        private ApplicationDbContext CreateDbContext() => new(_dbOptions);

        public void Dispose()
        {
            using var db = CreateDbContext();
            db.Database.EnsureDeleted();
        }

        private async Task<AuctionItem> SeedAuctionAsync(
            string sellerId = "seller-1",
            ItemStatus status = ItemStatus.Active,
            DateTime? endTime = null,
            decimal startingPrice = 50m)
        {
            using var db = CreateDbContext();

            var category = new Category { Name = "TestCat" };
            await db.Categories.AddAsync(category);

            var user = new ApplicationUser { Id = sellerId, UserName = sellerId, Email = $"{sellerId}@example.com" };
            if (await db.Users.FindAsync(sellerId) == null)
            {
                await db.Users.AddAsync(user);
            }

            var auction = new AuctionItem
            {
                Title = "Vintage Lamp",
                Description = "Vintage brass lamp",
                StartingPrice = startingPrice,
                CurrentPrice = startingPrice,
                StartTime = DateTime.UtcNow.AddHours(-5),
                EndTime = endTime ?? DateTime.UtcNow.AddMinutes(-5), // Expired by default
                Status = status,
                SellerId = sellerId,
                CategoryId = category.Id,
                AntiSnipingMinutes = 5
            };

            await db.AuctionItems.AddAsync(auction);
            await db.SaveChangesAsync();
            return auction;
        }

        [Fact]
        public async Task CloseExpiredAuctions_WhenAuctionExpired_SetsStatusToCompleted()
        {
            var auction = await SeedAuctionAsync();

            await _closerService.CloseExpiredAuctionsAsync(CancellationToken.None);

            using var db = CreateDbContext();
            var updated = await db.AuctionItems.FindAsync(auction.Id);
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(ItemStatus.Completed);
        }

        [Fact]
        public async Task CloseExpiredAuctions_WithWinningBid_DispatchesWonAlertToWinner_AndEndedAlertToSeller()
        {
            var auction = await SeedAuctionAsync(sellerId: "seller-1");

            var winnerId = "winner-99";
            using (var db = CreateDbContext())
            {
                var winningBid = new Bid
                {
                    AuctionItemId = auction.Id,
                    BidderId = winnerId,
                    BidAmount = 150m,
                    BidTime = DateTime.UtcNow.AddMinutes(-10),
                    IsWinningBid = true
                };
                await db.Bids.AddAsync(winningBid);
                await db.SaveChangesAsync();
            }

            await _closerService.CloseExpiredAuctionsAsync(CancellationToken.None);

            // Verify status
            using (var db = CreateDbContext())
            {
                var updated = await db.AuctionItems.FindAsync(auction.Id);
                updated!.Status.Should().Be(ItemStatus.Completed);
            }

            // Verify notification to winner
            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                winnerId,
                "Congratulations! You won the auction!",
                It.Is<string>(msg => msg.Contains("150.00") && msg.Contains("Vintage Lamp")),
                NotificationType.AuctionWon,
                auction.Id), Times.Once);

            // Verify notification to seller
            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                "seller-1",
                "Your auction has ended!",
                It.Is<string>(msg => msg.Contains("150.00")),
                NotificationType.AuctionEnded,
                auction.Id), Times.Once);
        }

        [Fact]
        public async Task CloseExpiredAuctions_WithNoBids_DispatchesEndedAlertToSeller_SpecifyingZeroBids()
        {
            var auction = await SeedAuctionAsync(sellerId: "seller-1");

            await _closerService.CloseExpiredAuctionsAsync(CancellationToken.None);

            // Verify notification to seller specifying no bids
            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                "seller-1",
                "Your auction has ended",
                It.Is<string>(msg => msg.Contains("no bids placed")),
                NotificationType.AuctionEnded,
                auction.Id), Times.Once);

            // Winner notification should never have been dispatched
            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationType.AuctionWon,
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task CloseExpiredAuctions_WhenAuctionNotExpired_DoesNotCloseOrNotify()
        {
            // EndTime 2 hours in the future
            var activeAuction = await SeedAuctionAsync(endTime: DateTime.UtcNow.AddHours(2));

            await _closerService.CloseExpiredAuctionsAsync(CancellationToken.None);

            using var db = CreateDbContext();
            var updated = await db.AuctionItems.FindAsync(activeAuction.Id);
            updated!.Status.Should().Be(ItemStatus.Active);

            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationType.AuctionWon,
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task CloseExpiredAuctions_WhenNotificationFails_StillMarksAuctionCompleted()
        {
            var auction = await SeedAuctionAsync();

            _mockNotificationService.Setup(n => n.CreateAndSendNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("SignalR timeout"));

            // Act: should not throw, catches error and completes auction
            await _closerService.CloseExpiredAuctionsAsync(CancellationToken.None);

            using var db = CreateDbContext();
            var updated = await db.AuctionItems.FindAsync(auction.Id);
            updated!.Status.Should().Be(ItemStatus.Completed);
        }
    }
}
