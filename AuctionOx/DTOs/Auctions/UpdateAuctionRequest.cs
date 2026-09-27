using System;
using System.ComponentModel.DataAnnotations;

namespace AuctionOx.DTOs.Auctions
{
    public class UpdateAuctionRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public decimal? BuyItNowPrice { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int CategoryId { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}
