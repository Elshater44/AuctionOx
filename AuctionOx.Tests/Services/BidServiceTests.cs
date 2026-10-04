using AuctionOx.Data;
using AuctionOx.DTOs.Bids;
using AuctionOx.Exceptions;
using AuctionOx.Mappings;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Services.Implementations;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionOx.Tests.Services
{
    public class BidServiceTests : IDisposable
    {
        // =====================================================================
        // FIELDS — these are shared by all tests, created fresh per test
        // =====================================================================
        private readonly BidService _bidService;
        private readonly ApplicationDbContext _dbContext;

        // AutoMapper config is expensive to compile. Making it static means
        // it compiles once and is reused across every test in this class.
        private static readonly IMapper _mapper = BuildMapper();

        private static IMapper BuildMapper()
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<MappingProfile>();
            var config = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        // =====================================================================
        // CONSTRUCTOR — xUnit creates a new instance of this class for EACH test.
        // So every test gets its own fresh database and service.
        // =====================================================================
        public BidServiceTests()
        {
            // Fresh in-memory database with a unique name (GUID) per test
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(dbOptions);

            // Wire up real UnitOfWork and real BidService — no fakes needed!
            var unitOfWork = new UnitOfWork(_dbContext);
            var logger = NullLogger<BidService>.Instance;
            _bidService = new BidService(unitOfWork, _mapper, logger);
        }

        // =====================================================================
        // CLEANUP — xUnit calls this after every test to release memory.
        // =====================================================================
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        // =====================================================================
        // HELPER — creates a valid, active auction already saved in the database.
        // In your tests, call this in the Arrange step so you have an auction to bid on.
        //
        // Usage:   var auction = await SeedActiveAuctionAsync("seller123", currentPrice: 100m);
        // Returns: the saved AuctionItem (with its auto-generated Id).
        // =====================================================================
        private async Task<AuctionItem> SeedActiveAuctionAsync(
            string sellerId,
            decimal currentPrice = 100m,
            decimal? buyItNowPrice = null)
        {
            var auction = new AuctionItem
            {
                Title = "Test Auction",
                Description = "A test item",
                StartingPrice = currentPrice,
                CurrentPrice = currentPrice,
                BuyItNowPrice = buyItNowPrice,
                StartTime = DateTime.UtcNow.AddMinutes(-30),   // Started 30 min ago
                EndTime = DateTime.UtcNow.AddDays(1),          // Ends tomorrow
                Status = ItemStatus.Active,
                SellerId = sellerId,
                CategoryId = 1,
                AntiSnipingMinutes = 5
            };

            _dbContext.Set<AuctionItem>().Add(auction);
            await _dbContext.SaveChangesAsync();
            return auction;
        }


        // =================================================================
        //  YOUR ASSIGNMENT — Complete the 5 tests below.
        //  
        //  For each test:
        //    1. Read the method name — it tells you EXACTLY what scenario to set up.
        //    2. Read the HINTs above it.
        //    3. Fill in the Arrange / Act / Assert sections.
        //    4. Run:  dotnet test
        //
        //  When you're done, all 5 should be green!
        // =================================================================


        // ── Test 1 ──────────────────────────────────────────────────────
        // HINT: Use [Fact] — single scenario, no parameters needed.
        // The database is empty, so any auctionId you pass won't exist.
        //
        // HINT for asserting exceptions:
        //   await FluentActions
        //       .Awaiting(() => _bidService.PlaceBidAsync(999, "user1", new PlaceBidRequest { BidAmount = 50m }))
        //       .Should()
        //       .ThrowAsync<BusinessRuleException>();
        [Fact]
        public async Task PlaceBidAsync_WhenAuctionDoesNotExist_ShouldThrowBusinessRuleException()
        {
            // Arrange — nothing to arrange, the database is empty!

            // Act & Assert
            await FluentActions.Awaiting(() => _bidService.PlaceBidAsync(999, "user1", new PlaceBidRequest { BidAmount = 50m }))
                .Should()
                .ThrowAsync<BusinessRuleException>();
        }


        // ── Test 2 ──────────────────────────────────────────────────────
        // HINT: Use [Fact].
        // Use SeedActiveAuctionAsync with sellerId = "seller1".
        // Then call PlaceBidAsync with the SAME userId "seller1".
        [Fact]
        public async Task PlaceBidAsync_WhenSellerBidsOnOwnAuction_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var auction = await SeedActiveAuctionAsync(sellerId: "seller1");

            // Act & Assert
            await FluentActions.Awaiting(() => _bidService.PlaceBidAsync(auction.Id, "seller1", new PlaceBidRequest { BidAmount = 50m })).Should().ThrowAsync<BusinessRuleException>();
        }


        // ── Test 3 ──────────────────────────────────────────────────────
        // HINT: Use [Theory] with [InlineData].
        // Seed an auction with currentPrice = 100m.
        // Test two cases:
        //   [InlineData(100)]  → bid equals current price (should fail)
        //   [InlineData(50)]   → bid is below current price (should fail)
        [Theory]
        [InlineData(100)]
        [InlineData(50)]
        public async Task PlaceBidAsync_WhenBidIsTooLow_ShouldThrowBusinessRuleException(decimal bidAmount)
        {
            // Arrange: auction current price is 100m
            var auction = await SeedActiveAuctionAsync(sellerId: "seller2", currentPrice: 100m);
            // Act & Assert: buyer1 bids bidAmount (either 100 or 50)
            await FluentActions.Awaiting(() => _bidService.PlaceBidAsync(auction.Id, "buyer1", new PlaceBidRequest { BidAmount = bidAmount }))
                .Should().ThrowAsync<BusinessRuleException>();
        }

        // ── Test 4 ──────────────────────────────────────────────────────
        // HINT: Use [Fact]. This is the HAPPY PATH — everything works correctly.
        // Seed an auction with currentPrice = 100m.
        // Place a valid bid of 150m from a different user (not the seller).
        //
        // Then assert TWO things:
        //   1. The returned BidDto has the correct BidAmount.
        //   2. The auction's CurrentPrice in the database actually changed to 150m.
        //      You can query the DB like this:
        //      var updatedAuction = await _dbContext.Set<AuctionItem>().FindAsync(auction.Id);
        //      updatedAuction!.CurrentPrice.Should().Be(150m);
        [Fact]
        public async Task PlaceBidAsync_WhenBidIsValid_ShouldSaveBidAndUpdateAuctionPrice()
        {
            // Arrange
            var auction = await SeedActiveAuctionAsync("sellerHappy1", 100m);
            // Act
            var result = await _bidService.PlaceBidAsync(auction.Id, "seller1", new PlaceBidRequest { BidAmount = 150m });
            // Assert
            result.Should().NotBeNull();
            result.BidAmount.Should().Be(150m);

            var updatedAuction = await _dbContext.Set<AuctionItem>().FindAsync(auction.Id);
            updatedAuction.Should().NotBeNull();
            updatedAuction!.CurrentPrice.Should().Be(150m);
        }


        // ── Test 5 (BONUS CHALLENGE) ────────────────────────────────────
        // HINT: Use [Fact].
        // Seed an auction with currentPrice = 100m AND buyItNowPrice = 200m.
        // Place a bid of 200m (or higher).
        //
        // Assert that:
        //   1. The auction Status changed to ItemStatus.Completed.
        //   2. The auction CurrentPrice equals the BuyItNowPrice (200m).
        [Fact]
        public async Task PlaceBidAsync_WhenBidMeetsBuyItNow_ShouldCompleteAuction()
        {
            // Arrange
            var auction = await SeedActiveAuctionAsync("sellerLast1", currentPrice: 100m, buyItNowPrice: 200m);
            // Act
            var res = await _bidService.PlaceBidAsync(auction.Id, "Buyer1", new PlaceBidRequest { BidAmount = 300m });
            // Assert
            // 1. The returned bid should be clamped to the BuyItNowPrice (200m)
            res.BidAmount.Should().Be(200m);
            // 2. The auction in the database should be Completed with CurrentPrice = 200m
            var updatedAuction = await _dbContext.Set<AuctionItem>().FindAsync(auction.Id);
            updatedAuction.Should().NotBeNull();
            updatedAuction!.Status.Should().Be(ItemStatus.Completed);
            updatedAuction.CurrentPrice.Should().Be(200m);


        }
    }
}
