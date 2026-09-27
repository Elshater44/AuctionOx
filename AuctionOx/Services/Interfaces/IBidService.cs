using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionOx.DTOs.Bids;

namespace AuctionOx.Services.Interfaces
{
    public interface IBidService
    {
        Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId);
        Task<BidDto> PlaceBidAsync(int auctionId, string userId, PlaceBidRequest request);
        Task<List<BidDto>> GetMyBidsAsync(string userId);
    }
}
