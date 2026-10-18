using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.DTOs.Common;
using AuctionOx.DTOs.Notifications;
using AuctionOx.Hubs;
using AuctionOx.Hubs.Clients;
using AuctionOx.Models;
using AuctionOx.Repositories.Interfaces;
using AuctionOx.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionOx.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<NotificationService> logger,
            IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto> CreateAndSendNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            int? auctionItemId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                AuctionItemId = auctionItemId,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            var dto = _mapper.Map<NotificationDto>(notification);

            try
            {
                await _hubContext.Clients.User(userId).ReceiveNotification(dto);

                var unreadCount = await _unitOfWork.Notifications.Query()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();

                await _hubContext.Clients.User(userId).UpdateUnreadCount(unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push SignalR notification to user {UserId}", userId);
            }

            return dto;
        }

        public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
            string userId,
            int pageNumber,
            int pageSize,
            bool? unreadOnly = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _unitOfWork.Notifications.Query()
                .Where(n => n.UserId == userId);

            if (unreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<NotificationDto>
            {
                Items = _mapper.Map<List<NotificationDto>>(items),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _unitOfWork.Notifications.Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.CompleteAsync();

                try
                {
                    var unreadCount = await GetUnreadCountAsync(userId);
                    await _hubContext.Clients.User(userId).UpdateUnreadCount(unreadCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast updated unread count to user {UserId}", userId);
                }
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _unitOfWork.Notifications.Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Count == 0)
            {
                return 0;
            }

            foreach (var item in unreadNotifications)
            {
                item.IsRead = true;
                _unitOfWork.Notifications.Update(item);
            }

            await _unitOfWork.CompleteAsync();

            try
            {
                await _hubContext.Clients.User(userId).UpdateUnreadCount(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast zero unread count to user {UserId}", userId);
            }

            return unreadNotifications.Count;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            _unitOfWork.Notifications.Remove(notification);
            await _unitOfWork.CompleteAsync();

            try
            {
                var unreadCount = await GetUnreadCountAsync(userId);
                await _hubContext.Clients.User(userId).UpdateUnreadCount(unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast updated unread count after deletion to user {UserId}", userId);
            }

            return true;
        }
    }
}
