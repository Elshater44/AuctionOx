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

        private async Task CloseExpiredAuctionsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;

            var expiredAuctions = await unitOfWork.Auctions.Query()
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
            }

            await unitOfWork.CompleteAsync();
            _logger.LogInformation("Successfully closed {Count} expired auctions.", expiredAuctions.Count);
        }
    }
}
