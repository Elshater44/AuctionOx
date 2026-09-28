using System;


namespace AuctionOx.DTOs.Auctions
{
    public class UpdateAuctionRequest
    {
        
        public string Title { get; set; } = string.Empty;

        
        public string Description { get; set; } = string.Empty;

        public decimal? BuyItNowPrice { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int CategoryId { get; set; }

        
        public string? ImageUrl { get; set; }
    }
}
