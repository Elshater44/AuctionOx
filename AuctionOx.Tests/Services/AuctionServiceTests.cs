using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Auctions;
using AuctionOx.Exceptions;
using AuctionOx.Mappings;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Services.Implementations;
using AuctionOx.Services.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class AuctionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly AuctionService _auctionService;
        private readonly UnitOfWork _unitOfWork;

        private static IMapper BuildMapper()
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<MappingProfile>();
            var config = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        public AuctionServiceTests()
        {
            _mapper = BuildMapper();

            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(dbOptions);
            _unitOfWork = new UnitOfWork(_dbContext);

            var logger = NullLogger<AuctionService>.Instance;
            _mockNotificationService = new Mock<INotificationService>();

            _auctionService = new AuctionService(
                _unitOfWork,
                _mapper,
                logger,
                _mockNotificationService.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        private async Task<Category> SeedCategoryAsync(string name = "Antiques")
        {
            var category = new Category { Name = name, Description = "Category desc" };
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        private async Task<ApplicationUser> SeedUserAsync(string userId = "seller-1")
        {
            var existing = await _dbContext.Users.FindAsync(userId);
            if (existing != null) return existing;

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId}@example.com",
                Email = $"{userId}@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<AuctionItem> SeedAuctionAsync(
            string sellerId = "seller-1",
            ItemStatus status = ItemStatus.Active,
            decimal startingPrice = 50m,
            decimal? buyItNowPrice = 150m,
            int? categoryId = null)
        {
            await SeedUserAsync(sellerId);

            if (!categoryId.HasValue)
            {
                var cat = await SeedCategoryAsync();
                categoryId = cat.Id;
            }

            var auction = new AuctionItem
            {
                Title = "Vintage Watch",
                Description = "Original vintage watch in great condition.",
                StartingPrice = startingPrice,
                CurrentPrice = startingPrice,
                BuyItNowPrice = buyItNowPrice,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = status,
                SellerId = sellerId,
                CategoryId = categoryId.Value,
                AntiSnipingMinutes = 5
            };

            await _dbContext.AuctionItems.AddAsync(auction);
            await _dbContext.SaveChangesAsync();
            return auction;
        }

        // =====================================================================
        // 1. CREATE AUCTION TESTS
        // =====================================================================

        [Fact]
        public async Task CreateAuction_WithValidData_ReturnsAuctionDto_AndSetsStatusActive()
        {
            await SeedUserAsync("seller-1");
            var category = await SeedCategoryAsync("Collectibles");
            var request = new CreateAuctionRequest
            {
                Title = "Rare Comic Book",
                Description = "First edition comic book.",
                StartingPrice = 100m,
                BuyItNowPrice = 300m,
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(3),
                CategoryId = category.Id,
                AntiSnipingMinutes = 5
            };

            var result = await _auctionService.CreateAuctionAsync("seller-1", request);

            result.Should().NotBeNull();
            result.Title.Should().Be("Rare Comic Book");
            result.CurrentPrice.Should().Be(100m);
            result.Status.Should().Be(ItemStatus.Active);
            result.SellerId.Should().Be("seller-1");
        }

        [Fact]
        public async Task CreateAuction_WhenStartTimeAfterEndTime_ThrowsBusinessRuleException()
        {
            var category = await SeedCategoryAsync();
            var request = new CreateAuctionRequest
            {
                Title = "Item",
                Description = "Desc",
                StartingPrice = 50m,
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(1),
                CategoryId = category.Id
            };

            var act = () => _auctionService.CreateAuctionAsync("seller-1", request);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Start time must be before end time.");
        }

        [Fact]
        public async Task CreateAuction_WhenBuyItNowLowerOrEqualToStartingPrice_ThrowsBusinessRuleException()
        {
            var category = await SeedCategoryAsync();
            var request = new CreateAuctionRequest
            {
                Title = "Item",
                Description = "Desc",
                StartingPrice = 50m,
                BuyItNowPrice = 50m,
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(1),
                CategoryId = category.Id
            };

            var act = () => _auctionService.CreateAuctionAsync("seller-1", request);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Buy It Now price must be greater than the starting price.");
        }

        [Fact]
        public async Task CreateAuction_WhenCategoryDoesNotExist_ThrowsBusinessRuleException()
        {
            var request = new CreateAuctionRequest
            {
                Title = "Item",
                Description = "Desc",
                StartingPrice = 50m,
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(1),
                CategoryId = 9999
            };

            var act = () => _auctionService.CreateAuctionAsync("seller-1", request);
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("The specified category does not exist.");
        }

        // =====================================================================
        // 2. UPDATE AUCTION TESTS
        // =====================================================================

        [Fact]
        public async Task UpdateAuction_WithoutBids_AllowsUpdatingAllFields()
        {
            var auction = await SeedAuctionAsync("seller-1");
            var updateRequest = new UpdateAuctionRequest
            {
                Title = "Renamed Title",
                Description = "New Description",
                BuyItNowPrice = 200m,
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(5),
                CategoryId = auction.CategoryId
            };

            var updated = await _auctionService.UpdateAuctionAsync(auction.Id, "seller-1", updateRequest);

            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Renamed Title");
            updated.BuyItNowPrice.Should().Be(200m);
            updated.Description.Should().Be("New Description");
        }

        [Fact]
        public async Task UpdateAuction_WithBidsPlaced_PreservesFinancialTerms_OnlyUpdatesCosmetics()
        {
            var auction = await SeedAuctionAsync("seller-1", startingPrice: 50m, buyItNowPrice: 150m);
            
            // Add a bid
            var bid = new Bid
            {
                AuctionItemId = auction.Id,
                BidderId = "bidder-1",
                BidAmount = 60m,
                BidTime = DateTime.UtcNow,
                IsWinningBid = true
            };
            await _dbContext.Bids.AddAsync(bid);
            await _dbContext.SaveChangesAsync();

            var updateRequest = new UpdateAuctionRequest
            {
                Title = "Attempted Title Modification",
                Description = "Updated description text.",
                ImageUrl = "https://example.com/new-pic.jpg",
                BuyItNowPrice = 500m, // Should be ignored
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(10),
                CategoryId = auction.CategoryId
            };

            var result = await _auctionService.UpdateAuctionAsync(auction.Id, "seller-1", updateRequest);

            result.Should().NotBeNull();
            result!.Description.Should().Be("Updated description text.");
            result.ImageUrl.Should().Be("https://example.com/new-pic.jpg");

            // Verify financial terms were preserved
            result.BuyItNowPrice.Should().Be(150m);
            result.Title.Should().Be("Vintage Watch");
        }

        [Fact]
        public async Task UpdateAuction_WhenCalledByNonOwnerAndNotAdmin_ReturnsNull()
        {
            var auction = await SeedAuctionAsync("seller-1");
            var updateRequest = new UpdateAuctionRequest
            {
                Title = "Unauthorized Edit",
                Description = "Desc",
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(1),
                CategoryId = auction.CategoryId
            };

            var result = await _auctionService.UpdateAuctionAsync(auction.Id, "different-user", updateRequest, isAdmin: false);
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAuction_WhenCalledByAdmin_AllowsUpdatingEvenIfNotOwner()
        {
            var auction = await SeedAuctionAsync("seller-1");
            var updateRequest = new UpdateAuctionRequest
            {
                Title = "Admin Corrected Title",
                Description = "Desc",
                StartTime = DateTime.UtcNow.AddMinutes(5),
                EndTime = DateTime.UtcNow.AddDays(1),
                CategoryId = auction.CategoryId
            };

            var result = await _auctionService.UpdateAuctionAsync(auction.Id, "admin-user", updateRequest, isAdmin: true);
            result.Should().NotBeNull();
            result!.Title.Should().Be("Admin Corrected Title");
        }

        // =====================================================================
        // 3. DELETE & ADMIN CONTROLS TESTS
        // =====================================================================

        [Fact]
        public async Task DeleteAuction_ByOwner_SoftDeletesAuction()
        {
            var auction = await SeedAuctionAsync("seller-1");

            var success = await _auctionService.DeleteAuctionAsync(auction.Id, "seller-1");

            success.Should().BeTrue();
            
            // Standard query filter should exclude soft-deleted items
            var retrieved = await _dbContext.AuctionItems.FirstOrDefaultAsync(a => a.Id == auction.Id);
            retrieved.Should().BeNull();

            // Ignore query filter should confirm it is soft-deleted
            var softDeleted = await _dbContext.AuctionItems.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == auction.Id);
            softDeleted.Should().NotBeNull();
            softDeleted!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAuction_ByNonOwner_ReturnsFalse()
        {
            var auction = await SeedAuctionAsync("seller-1");

            var success = await _auctionService.DeleteAuctionAsync(auction.Id, "wrong-user");
            success.Should().BeFalse();
        }

        [Fact]
        public async Task SuspendAuction_AndResumeAuction_TogglesStatus()
        {
            var auction = await SeedAuctionAsync("seller-1");

            var suspendSuccess = await _auctionService.SuspendAuctionAsync(auction.Id);
            suspendSuccess.Should().BeTrue();
            var suspended = await _dbContext.AuctionItems.FirstOrDefaultAsync(a => a.Id == auction.Id);
            suspended!.Status.Should().Be(ItemStatus.Suspended);

            var resumeSuccess = await _auctionService.ResumeAuctionAsync(auction.Id);
            resumeSuccess.Should().BeTrue();
            var resumed = await _dbContext.AuctionItems.FirstOrDefaultAsync(a => a.Id == auction.Id);
            resumed!.Status.Should().Be(ItemStatus.Active);
        }

        // =====================================================================
        // 4. BUY IT NOW TESTS
        // =====================================================================

        [Fact]
        public async Task BuyItNow_ValidConditions_ClosesAuction_AndDispatchesNotifications()
        {
            var auction = await SeedAuctionAsync("seller-1", buyItNowPrice: 200m);
            var buyerId = "buyer-123";

            var success = await _auctionService.BuyItNowAsync(auction.Id, buyerId);

            success.Should().BeTrue();

            var updated = await _dbContext.AuctionItems.Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == auction.Id);
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(ItemStatus.Completed);
            updated.CurrentPrice.Should().Be(200m);
            updated.Bids.Should().ContainSingle(b => b.BidderId == buyerId && b.IsWinningBid && b.BidAmount == 200m);

            // Assert notifications were dispatched to buyer and seller
            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                buyerId,
                "Congratulations! Item Won!",
                It.Is<string>(msg => msg.Contains("Buy It Now")),
                NotificationType.AuctionWon,
                auction.Id), Times.Once);

            _mockNotificationService.Verify(n => n.CreateAndSendNotificationAsync(
                "seller-1",
                "Item Sold!",
                It.Is<string>(msg => msg.Contains("Buy It Now")),
                NotificationType.AuctionEnded,
                auction.Id), Times.Once);
        }

        [Fact]
        public async Task BuyItNow_WhenSellerBuysOwnAuction_ThrowsBusinessRuleException()
        {
            var auction = await SeedAuctionAsync("seller-1", buyItNowPrice: 200m);

            var act = () => _auctionService.BuyItNowAsync(auction.Id, "seller-1");
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("You cannot buy your own auction.");
        }

        [Fact]
        public async Task BuyItNow_WhenAuctionAlreadyEnded_ThrowsBusinessRuleException()
        {
            var auction = await SeedAuctionAsync("seller-1", buyItNowPrice: 200m);
            auction.EndTime = DateTime.UtcNow.AddMinutes(-5);
            await _dbContext.SaveChangesAsync();

            var act = () => _auctionService.BuyItNowAsync(auction.Id, "buyer-1");
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("This auction has already ended.");
        }

        [Fact]
        public async Task BuyItNow_WhenCurrentBidMeetsOrExceedsBuyNow_ThrowsBusinessRuleException()
        {
            var auction = await SeedAuctionAsync("seller-1", startingPrice: 100m, buyItNowPrice: 150m);
            auction.CurrentPrice = 160m;
            await _dbContext.SaveChangesAsync();

            var act = () => _auctionService.BuyItNowAsync(auction.Id, "buyer-1");
            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Current bid price already meets or exceeds the Buy It Now price.");
        }

        // =====================================================================
        // 5. QUERY & FILTERING TESTS
        // =====================================================================

        [Fact]
        public async Task GetAuctions_WithFilters_ReturnsFilteredPagedResult()
        {
            var cat1 = await SeedCategoryAsync("Category A");
            var cat2 = await SeedCategoryAsync("Category B");

            await SeedAuctionAsync("seller-1", ItemStatus.Active, categoryId: cat1.Id);
            await SeedAuctionAsync("seller-1", ItemStatus.Active, categoryId: cat2.Id);
            await SeedAuctionAsync("seller-1", ItemStatus.Completed, categoryId: cat1.Id);

            var result = await _auctionService.GetAuctionsAsync(
                categoryId: cat1.Id,
                status: "Active",
                search: null,
                sortBy: null,
                pageNumber: 1,
                pageSize: 10);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle();
            result.Items[0].CategoryName.Should().Be("Category A");
        }
    }
}
