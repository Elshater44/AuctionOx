using System;
using System.Collections.Generic;
using System.Linq;
using AuctionOx.Helpers;
using AuctionOx.Models;
using FluentAssertions;
using Xunit;

namespace AuctionOx.Tests
{
    public class AuctionBiddingHelperTests
    {
        [Fact]
        public void CreateWinningBid_BuyItNow_CompletesAuction()
        {
            // Arrange
            var auction = new AuctionItem
            {
                Id = 1,
                CurrentPrice = 100m,
                BuyItNowPrice = 200m,
                Status = ItemStatus.Active,
                EndTime = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var bid = AuctionBiddingHelper.CreateWinningBidAndCloseOrExtend(auction, "user1", 200m, isBuyItNow: true);

            // Assert
            bid.IsWinningBid.Should().BeTrue();
            bid.BidAmount.Should().Be(200m);
            auction.CurrentPrice.Should().Be(200m);
            auction.Status.Should().Be(ItemStatus.Completed);
            auction.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateWinningBid_NormalBid_UpdatesCurrentPriceAndAntiSniping()
        {
            // Arrange
            var originalEndTime = DateTime.UtcNow.AddMinutes(2); // 2 mins remaining
            var auction = new AuctionItem
            {
                Id = 1,
                CurrentPrice = 100m,
                Status = ItemStatus.Active,
                EndTime = originalEndTime,
                AntiSnipingMinutes = 5
            };

            // Act
            var bid = AuctionBiddingHelper.CreateWinningBidAndCloseOrExtend(auction, "user2", 150m, isBuyItNow: false);

            // Assert
            bid.IsWinningBid.Should().BeTrue();
            auction.CurrentPrice.Should().Be(150m);
            auction.Status.Should().Be(ItemStatus.Active); // Still active
            auction.EndTime.Should().BeAfter(originalEndTime); // Extended!
            auction.EndTime.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateWinningBid_NormalBid_UnmarksPreviousWinningBids()
        {
            // Arrange
            var prevBid1 = new Bid { Id = 1, IsWinningBid = true, BidderId = "user1" };
            var prevBid2 = new Bid { Id = 2, IsWinningBid = false, BidderId = "user2" };
            
            var auction = new AuctionItem
            {
                Id = 1,
                Bids = new List<Bid> { prevBid1, prevBid2 },
                EndTime = DateTime.UtcNow.AddHours(1) // Long time, no anti-sniping needed
            };

            // Act
            var newBid = AuctionBiddingHelper.CreateWinningBidAndCloseOrExtend(auction, "user3", 200m, isBuyItNow: false);

            // Assert
            prevBid1.IsWinningBid.Should().BeFalse();
            prevBid2.IsWinningBid.Should().BeFalse();
            newBid.IsWinningBid.Should().BeTrue();
            // In the real system, newBid is added to the database via UnitOfWork afterwards.
        }
    }
}
