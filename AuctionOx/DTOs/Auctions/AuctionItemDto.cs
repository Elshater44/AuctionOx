using System;
using AuctionOx.Models;

namespace AuctionOx.DTOs.Auctions
{
    public class AuctionItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? BuyItNowPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ItemStatus Status { get; set; }
        public string? ImageUrl { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int BidCount { get; set; }
    }
}
