using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuctionOx.Models;
using AuctionOx.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionOx.BackgroundServices
{
    public class AuctionCloserService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionCloserService> _logger;

        public AuctionCloserService(IServiceProvider serviceProvider, ILogger<AuctionCloserService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auction Closer Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CloseExpiredAuctionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while closing expired auctions.");
                }

                // Run every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Auction Closer Background Service is stopping.");
        }

        public async Task CloseExpiredAuctionsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var notificationService = scope.ServiceProvider.GetService<AuctionOx.Services.Interfaces.INotificationService>();

            var now = DateTime.UtcNow;

            var expiredAuctions = await unitOfWork.Auctions.Query()
                .Include(a => a.Bids)
                .Where(a => a.Status == ItemStatus.Active && a.EndTime <= now)
                .ToListAsync(cancellationToken);

            if (!expiredAuctions.Any())
            {
                return;
            }

            foreach (var auction in expiredAuctions)
            {
                auction.Status = ItemStatus.Completed;
                unitOfWork.Auctions.Update(auction);
                _logger.LogInformation("Auction {AuctionId} has ended and is marked as Completed.", auction.Id);

                if (notificationService != null)
                {
                    try
                    {
                        var winningBid = auction.Bids?
                            .OrderByDescending(b => b.BidAmount)
                            .FirstOrDefault(b => b.IsWinningBid) 
                            ?? auction.Bids?.OrderByDescending(b => b.BidAmount).FirstOrDefault();

                        if (winningBid != null)
                        {
                            // Winner notification
                            await notificationService.CreateAndSendNotificationAsync(
                                winningBid.BidderId,
                                "Congratulations! You won the auction!",
                                $"You won \"{auction.Title}\" with a highest bid of {winningBid.BidAmount:C}!",
                                NotificationType.AuctionWon,
                                auction.Id);

                            // Seller notification
                            await notificationService.CreateAndSendNotificationAsync(
                                auction.SellerId,
                                "Your auction has ended!",
                                $"Your auction \"{auction.Title}\" concluded with a winning bid of {winningBid.BidAmount:C}.",
                                NotificationType.AuctionEnded,
                                auction.Id);
                        }
                        else
                        {
                            // Seller notification without bids
                            await notificationService.CreateAndSendNotificationAsync(
                                auction.SellerId,
                                "Your auction has ended",
                                $"Your auction \"{auction.Title}\" concluded with no bids placed.",
                                NotificationType.AuctionEnded,
                                auction.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notifications for closed auction {AuctionId}", auction.Id);
                    }
                }
            }

            await unitOfWork.CompleteAsync();
            _logger.LogInformation("Successfully closed {Count} expired auctions.", expiredAuctions.Count);
        }
    }
}
