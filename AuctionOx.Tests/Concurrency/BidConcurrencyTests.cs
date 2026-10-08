using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Bids;
using AuctionOx.Exceptions;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using AuctionOx.Tests.Integration;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuctionOx.Tests.Concurrency
{
    public class BidConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public BidConcurrencyTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Creates a unique test user in the database so foreign keys are satisfied.
        /// </summary>
        private async Task<ApplicationUser> CreateTestUserAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return existing;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "Bidder",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Password123!");
            return user;
        }

        /// <summary>
        /// Creates an active auction in the test database.
        /// </summary>
        private async Task<AuctionItem> CreateTestAuctionAsync(string sellerId, decimal startingPrice = 100m)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var auction = new AuctionItem
            {
                Title = "Rare Collectible Coin",
                Description = "High concurrency test subject",
                StartingPrice = startingPrice,
                CurrentPrice = startingPrice,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = ItemStatus.Active,
                SellerId = sellerId,
                CategoryId = 1,
                AntiSnipingMinutes = 5
            };
            db.AuctionItems.Add(auction);
            await db.SaveChangesAsync();
            return auction;
        }

        /// <summary>
        /// Creates an independent service scope to simulate a distinct HTTP request thread.
        /// </summary>
        private IBidService GetScopedBidService(IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<IBidService>();
        }

        // =====================================================================
        // TEST ASSIGNMENTS
        // =====================================================================

        // ── Test 1: Simultaneous Bids with Ascending Amounts ─────────────
        // SCENARIO:
        //   An auction starts at $100.
        //   5 different bidders submit bids ($110, $120, $130, $140, $150) at the SAME time.
        [Fact]
        public async Task ConcurrentBids_WithAscendingAmounts_ShouldEndWithHighestBidAsCurrentPrice()
        {
            // Arrange
            var seller = await CreateTestUserAsync("seller@test.com");
            var user1 = await CreateTestUserAsync("user1@test.com");
            var user2 = await CreateTestUserAsync("user2@test.com");
            var user3 = await CreateTestUserAsync("user3@test.com");
            var user4 = await CreateTestUserAsync("user4@test.com");
            var user5 = await CreateTestUserAsync("user5@test.com");

            // Act
            var auction = await CreateTestAuctionAsync(seller.Id, 100m);

            var task1 = Task.Run(async () =>
            {
                try
                {
                    using var scope = _factory.Services.CreateScope();
                    var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
                    return await bidService.PlaceBidAsync(auction.Id, user1.Id, new PlaceBidRequest { BidAmount = 110m });
                }
                catch (BusinessRuleException)
                {
                    return null; // Outbid by a concurrent higher bid!
                }
            });

            var task2 = Task.Run(async () =>
            {
                try
                {
                    using var scope = _factory.Services.CreateScope();
                    var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
                    return await bidService.PlaceBidAsync(auction.Id, user2.Id, new PlaceBidRequest { BidAmount = 120m });
                }
                catch (BusinessRuleException)
                {
                    return null;
                }
            });

            var task3 = Task.Run(async () =>
            {
                try
                {
                    using var scope = _factory.Services.CreateScope();
                    var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
                    return await bidService.PlaceBidAsync(auction.Id, user3.Id, new PlaceBidRequest { BidAmount = 130m });
                }
                catch (BusinessRuleException)
                {
                    return null;
                }
            });

            var task4 = Task.Run(async () =>
            {
                try
                {
                    using var scope = _factory.Services.CreateScope();
                    var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
                    return await bidService.PlaceBidAsync(auction.Id, user4.Id, new PlaceBidRequest { BidAmount = 140m });
                }
                catch (BusinessRuleException)
                {
                    return null;
                }
            });

            var task5 = Task.Run(async () =>
            {
                try
                {
                    using var scope = _factory.Services.CreateScope();
                    var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
                    return await bidService.PlaceBidAsync(auction.Id, user5.Id, new PlaceBidRequest { BidAmount = 150m });
                }
                catch (BusinessRuleException)
                {
                    return null;
                }
            });

            await Task.WhenAll(task1, task2, task3, task4, task5);

            // Assert
            using var assertScope = _factory.Services.CreateScope();
            var db = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var updatedAuction = await db.AuctionItems.FindAsync(auction.Id);
            updatedAuction.Should().NotBeNull();
            updatedAuction!.CurrentPrice.Should().Be(150m);
        }

        // ── Test 2: Identical Bid Price Collision ─────────────────────────
        // SCENARIO:
        //   Two users see a $100 auction and click "Bid $120" at the exact same millisecond.
        //   Only ONE can succeed; the second one MUST be rejected with a BusinessRuleException
        //   (because by the time it runs, the current price is already $120!).
        //
        // HINT:
        //   1. Create two test users (userA and userB).
        //   2. Create an auction at 100m.
        //   3. Launch two Task.Run calls bidding $120 simultaneously.
        //   4. Catch exceptions or track results: exactly 1 should succeed, 1 should throw.
        //   5. Assert that the auction CurrentPrice is 120m, and there is only ONE winning bid.
        [Fact]
        public async Task ConcurrentBids_WithIdenticalAmount_OnlyOneBidShouldSucceed()
        {
            // Arrange

            // Act

            // Assert

        }
    }
}
