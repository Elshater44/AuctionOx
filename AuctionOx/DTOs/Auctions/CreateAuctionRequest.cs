using System;
using System.ComponentModel.DataAnnotations;

namespace AuctionOx.DTOs.Auctions
{
    public class CreateAuctionRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal StartingPrice { get; set; }

        public decimal? BuyItNowPrice { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int CategoryId { get; set; }

        public int AntiSnipingMinutes { get; set; } = 5;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}
