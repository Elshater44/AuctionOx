using System.Collections.Generic;
using AuctionOx.DTOs.Bids;

namespace AuctionOx.DTOs.Auctions
{
    public class AuctionDetailDto : AuctionItemDto
    {
        public List<BidDto> RecentBids { get; set; } = new List<BidDto>();
    }
}
