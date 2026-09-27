using System;

namespace AuctionOx.DTOs.Bids
{
    public class BidDto
    {
        public int Id { get; set; }
        public decimal BidAmount { get; set; }
        public DateTime BidTime { get; set; }
        public string BidderName { get; set; } = string.Empty;
        public bool IsWinningBid { get; set; }
    }
}
