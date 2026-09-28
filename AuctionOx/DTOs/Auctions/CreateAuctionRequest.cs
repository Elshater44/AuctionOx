using System;


namespace AuctionOx.DTOs.Auctions
{
    public class CreateAuctionRequest
    {
        
        public string Title { get; set; } = string.Empty;

        
        public string Description { get; set; } = string.Empty;

        public decimal StartingPrice { get; set; }

        public decimal? BuyItNowPrice { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int CategoryId { get; set; }

        public int AntiSnipingMinutes { get; set; } = 5;

        
        public string? ImageUrl { get; set; }
    }
}
