using AuctionOx.DTOs.Bids;

namespace AuctionOx.Hubs.Clients
{
    public interface IBiddingClient
    {
        Task ReceiveLiveBidData(List<BidDto> bidDtos);
    }
}