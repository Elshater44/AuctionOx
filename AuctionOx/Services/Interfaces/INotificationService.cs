using System.Threading.Tasks;
using AuctionOx.DTOs.Common;
using AuctionOx.DTOs.Notifications;
using AuctionOx.Models;

namespace AuctionOx.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAndSendNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            int? auctionItemId = null);

        Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
            string userId,
            int pageNumber,
            int pageSize,
            bool? unreadOnly = null);

        Task<int> GetUnreadCountAsync(string userId);

        Task<bool> MarkAsReadAsync(int notificationId, string userId);

        Task<int> MarkAllAsReadAsync(string userId);

        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    }
}
