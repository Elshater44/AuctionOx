using System.Threading.Tasks;
using AuctionOx.Hubs.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AuctionOx.Hubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
    }
}
