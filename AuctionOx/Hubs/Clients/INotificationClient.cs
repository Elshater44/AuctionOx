using System.Threading.Tasks;
using AuctionOx.DTOs.Notifications;

namespace AuctionOx.Hubs.Clients
{
    public interface INotificationClient
    {
        Task ReceiveNotification(NotificationDto notification);
        Task UpdateUnreadCount(int unreadCount);
    }
}
