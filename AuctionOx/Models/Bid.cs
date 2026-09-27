using System;
using System.ComponentModel.DataAnnotations;

namespace AuctionOx.Models
{
    public class Bid : BaseEntity
    {
        public decimal BidAmount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsWinningBid { get; set; }

        [Required]
        public string BidderId { get; set; } = string.Empty;
        public ApplicationUser? Bidder { get; set; }

        public int AuctionItemId { get; set; }
        public AuctionItem? AuctionItem { get; set; }
    }
}
