using System;
using AuctionOx.Models;

namespace AuctionOx.DTOs.Bids
{
    public class BidDto
    {
        public int Id { get; set; }
        public int AuctionItemId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal BidAmount { get; set; }
        public decimal CurrentAuctionPrice { get; set; }
        public DateTime BidTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string BidderId { get; set; } = string.Empty;
        public string BidderName { get; set; } = string.Empty;
        public bool IsWinningBid { get; set; }
        public ItemStatus? AuctionStatus { get; set; }
        public string? ImageUrl { get; set; }
    }
}
