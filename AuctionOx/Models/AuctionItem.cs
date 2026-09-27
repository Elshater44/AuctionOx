using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuctionOx.Models
{
    public class AuctionItem : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? BuyItNowPrice { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AntiSnipingMinutes { get; set; } = 5; // Default to 5 minutes extension
        public ItemStatus Status { get; set; } = ItemStatus.Draft;
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
}
