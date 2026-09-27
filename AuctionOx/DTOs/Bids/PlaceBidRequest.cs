using System.ComponentModel.DataAnnotations;

namespace AuctionOx.DTOs.Bids
{
    public class PlaceBidRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal BidAmount { get; set; }
    }
}
