using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuctionOx.DTOs.Bids;
using AuctionOx.Exceptions;
using AuctionOx.Helpers;
using AuctionOx.Hubs;
using AuctionOx.Hubs.Clients;
using AuctionOx.Models;
using AuctionOx.Repositories.Implementations;
using AuctionOx.Repositories.Interfaces;
using AuctionOx.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionOx.Services.Implementations
{
    public class BidService : IBidService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BidService> _logger;
        private readonly IHubContext<BiddingHub, IBiddingClient> _biddingHubContext;
        private readonly INotificationService? _notificationService;

        public BidService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BidService> logger,
            IHubContext<BiddingHub, IBiddingClient> biddingHubContext,
            INotificationService? notificationService = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _biddingHubContext = biddingHubContext;
            _notificationService = notificationService;
        }

        public async Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId)
        {
            var bids = await _unitOfWork.Bids.Query()
                .Include(b => b.Bidder)
                .Where(b => b.AuctionItemId == auctionId)
                .OrderByDescending(b => b.BidAmount)
                .ToListAsync();

            return _mapper.Map<List<BidDto>>(bids);
        }

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _auctionLocks = new();

        public async Task<BidDto> PlaceBidAsync(int auctionId, string userId, PlaceBidRequest request)
        {
            var semaphore = _auctionLocks.GetOrAdd(auctionId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                int maxRetries = 3;
                for (int retryCount = 0; retryCount < maxRetries; retryCount++)
                {
                    try
                    {
                        var auction = await _unitOfWork.Auctions.Query()
                            .Include(a => a.Bids)
                            .FirstOrDefaultAsync(a => a.Id == auctionId);

                        if (auction == null) throw new BusinessRuleException("Auction not found.");

                        if (auction.Status != ItemStatus.Active) throw new BusinessRuleException("Auction is not active.");

                        if (auction.SellerId == userId) throw new BusinessRuleException("Sellers cannot bid on their own auctions.");

                        // Auction must have started
                        if (DateTime.UtcNow < auction.StartTime) throw new BusinessRuleException("This auction has not started yet.");

                        if (DateTime.UtcNow > auction.EndTime) throw new BusinessRuleException("Auction has already ended.");

                        if (request.BidAmount <= auction.CurrentPrice)
                            throw new BusinessRuleException($"Bid must be higher than the current price of {auction.CurrentPrice:C}.");

                        bool isBuyItNow = auction.BuyItNowPrice.HasValue && request.BidAmount >= auction.BuyItNowPrice.Value;
                        if (isBuyItNow)
                        {
                            request.BidAmount = auction.BuyItNowPrice!.Value;
                        }

                        var previousWinningBid = auction.Bids?.OrderByDescending(b => b.BidAmount).FirstOrDefault(b => b.IsWinningBid) 
                            ?? auction.Bids?.OrderByDescending(b => b.BidAmount).FirstOrDefault();

                        var bid = AuctionBiddingHelper.CreateWinningBidAndCloseOrExtend(auction, userId, request.BidAmount, isBuyItNow);

                        await _unitOfWork.Bids.AddAsync(bid);
                        _unitOfWork.Auctions.Update(auction);
                        await _unitOfWork.CompleteAsync();

                        var bidWithNav = await _unitOfWork.Bids.Query()
                            .Include(b => b.Bidder)
                            .FirstOrDefaultAsync(b => b.Id == bid.Id);

                        _logger.LogInformation("Bid of {Amount} successfully placed on auction {AuctionId} by user {UserId}.", bid.BidAmount, auctionId, userId);
                        var liveBiddingData = await GetBidsForAuctionAsync(auctionId);
                        await _biddingHubContext.Clients.Group(auctionId.ToString()).ReceiveLiveBidData(liveBiddingData);

                        if (_notificationService != null)
                        {
                            try
                            {
                                // Alert prior highest bidder that they've been outbid
                                if (previousWinningBid != null && previousWinningBid.BidderId != userId)
                                {
                                    await _notificationService.CreateAndSendNotificationAsync(
                                        previousWinningBid.BidderId,
                                        "Outbid Alert!",
                                        $"You have been outbid on \"{auction.Title}\". New highest bid is {bid.BidAmount:C}.",
                                        NotificationType.Outbid,
                                        auction.Id);
                                }

                                // Alert seller
                                if (auction.SellerId != userId)
                                {
                                    if (isBuyItNow)
                                    {
                                        await _notificationService.CreateAndSendNotificationAsync(
                                            auction.SellerId,
                                            "Item Sold!",
                                            $"Your auction \"{auction.Title}\" was purchased via Buy It Now for {bid.BidAmount:C}.",
                                            NotificationType.AuctionEnded,
                                            auction.Id);

                                        await _notificationService.CreateAndSendNotificationAsync(
                                            userId,
                                            "Congratulations! Item Won!",
                                            $"You purchased \"{auction.Title}\" for {bid.BidAmount:C} via Buy It Now.",
                                            NotificationType.AuctionWon,
                                            auction.Id);
                                    }
                                    else
                                    {
                                        await _notificationService.CreateAndSendNotificationAsync(
                                            auction.SellerId,
                                            "New Bid Received!",
                                            $"A new bid of {bid.BidAmount:C} was placed on your auction \"{auction.Title}\".",
                                            NotificationType.NewBid,
                                            auction.Id);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send notification for bid placed on auction {AuctionId}", auctionId);
                            }
                        }

                        return _mapper.Map<BidDto>(bidWithNav ?? bid);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (retryCount == maxRetries - 1)
                        {
                            throw new BusinessRuleException("The auction was updated by another user. Please refresh and try again.");
                        }
                        if (_unitOfWork is UnitOfWork uow && uow.Context != null)
                        {
                            uow.Context.ChangeTracker.Clear();
                        }
                    }
                }

                throw new BusinessRuleException("Failed to place bid after multiple attempts.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<List<BidDto>> GetMyBidsAsync(string userId)
        {
            var bids = await _unitOfWork.Bids.Query()
                .Include(b => b.Bidder)
                .Include(b => b.AuctionItem)
                .Where(b => b.BidderId == userId)
                .OrderByDescending(b => b.BidTime)
                .ToListAsync();

            return _mapper.Map<List<BidDto>>(bids);
        }
    }
}
