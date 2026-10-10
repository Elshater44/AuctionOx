using AuctionOx.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace AuctionOx.Hubs
{
    public class BiddingHub : Hub<IBiddingClient>
    {
        public async Task JoinAuctionRoom(int auctionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, (auctionId).ToString());
        }

        public async Task LeaveAuctionRoom(int auctionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, auctionId.ToString());
        }

        //public async Task SendLiveAuctionData(AuctionDetailDto auctionDetailDto)
        //{
        //    await Clients.Group(auctionDetailDto.Id.ToString()).ReceiveLiveBidData(auctionDetailDto);
        //}
    }
}
