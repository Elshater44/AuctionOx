using System;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Bids;
using AuctionOx.Mappings;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Services.Implementations;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuctionOx.Tests.Services
{
    public class BidServiceTests
    {
        private readonly BidService _bidService;
        private readonly ApplicationDbContext _dbContext;

        public BidServiceTests()
        {
            // 1. Create a fresh In-Memory Database for each test run using a unique GUID
            // This guarantees test isolation (no data bleeds between tests).
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new ApplicationDbContext(dbOptions);

            // 2. Setup the real UnitOfWork wrapping our In-Memory DbContext
            var unitOfWork = new UnitOfWork(_dbContext);

            // 3. Setup real AutoMapper just like in Program.cs
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = mapperConfig.CreateMapper();

            // 4. Setup a dummy logger (we don't care about logging in these tests)
            var logger = NullLogger<BidService>.Instance;

            // 5. Instantiate the real service we are going to test
            _bidService = new BidService(unitOfWork, mapper, logger);
        }

        // HINT: Use [Fact] here since there's only one scenario: an ID that doesn't exist.
        // HINT: To assert exceptions in FluentAssertions, use:
        // await FluentActions.Awaiting(() => _bidService.Method()).Should().ThrowAsync<ExpectedException>();
        public async Task PlaceBidAsync_WhenAuctionDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            
            // Act
            
            // Assert
        }

        // HINT: Use [Fact]. Create an auction in `_dbContext`, give it a SellerId, 
        // then try to place a bid using the EXACT same SellerId.
        public async Task PlaceBidAsync_WhenSellerBidsOnOwnAuction_ShouldThrowBusinessRuleException()
        {
            // Arrange
            
            // Act
            
            // Assert
        }

        // HINT: Use [Theory] and [InlineData]. You want to test two cases:
        // 1. Bid is exactly EQUAL to the current price.
        // 2. Bid is LOWER than the current price.
        public async Task PlaceBidAsync_WhenBidIsLessThanOrEqualToCurrentPrice_ShouldThrowBusinessRuleException(decimal bidAmount)
        {
            // Arrange
            
            // Act
            
            // Assert
        }

        // HINT: Use [Fact]. This is the "Happy Path". Place a valid bid, and then
        // assert that the bid was returned correctly AND check `_dbContext.Auctions` 
        // to prove the CurrentPrice actually updated in the database!
        public async Task PlaceBidAsync_WhenBidIsValid_ShouldPersistBidAndUpdateAuctionPrice()
        {
            // Arrange
            
            // Act
            
            // Assert
        }
    }
}
