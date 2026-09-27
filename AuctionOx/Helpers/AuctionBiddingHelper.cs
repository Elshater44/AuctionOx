using System;
using System.Linq;
using AuctionOx.Models;

namespace AuctionOx.Helpers
{
    public static class AuctionBiddingHelper
    {
        public static Bid CreateWinningBidAndCloseOrExtend(AuctionItem auction, string userId, decimal bidAmount, bool isBuyItNow)
        {
            // Mark previous bids as non-winning
            if (auction.Bids != null)
            {
                foreach (var previousBid in auction.Bids.Where(b => b.IsWinningBid))
                {
                    previousBid.IsWinningBid = false;
                }
            }

            var bid = new Bid
            {
                AuctionItemId = auction.Id,
                BidAmount = bidAmount,
                BidTime = DateTime.UtcNow,
                BidderId = userId,
                IsWinningBid = true
            };

            auction.CurrentPrice = bid.BidAmount;

            if (isBuyItNow)
            {
                auction.Status = ItemStatus.Completed;
                auction.EndTime = DateTime.UtcNow;
            }
            else
            {
                // Anti-sniping
                var timeRemaining = auction.EndTime - DateTime.UtcNow;
                if (timeRemaining.TotalMinutes < auction.AntiSnipingMinutes)
                {
                    auction.EndTime = DateTime.UtcNow.AddMinutes(auction.AntiSnipingMinutes);
                }
            }

            return bid;
        }
    }
}
