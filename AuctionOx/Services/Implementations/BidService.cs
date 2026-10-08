using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Bids;
using AuctionOx.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AuctionOx.Services.Implementations
{
    public class BidService : Interfaces.IBidService
    {
        private readonly AuctionOx.Repositories.Interfaces.IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly Microsoft.Extensions.Logging.ILogger<BidService> _logger;

        public BidService(AuctionOx.Repositories.Interfaces.IUnitOfWork unitOfWork, IMapper mapper, Microsoft.Extensions.Logging.ILogger<BidService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
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

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Threading.SemaphoreSlim> _auctionLocks = new();

        public async Task<BidDto> PlaceBidAsync(int auctionId, string userId, PlaceBidRequest request)
        {
            var semaphore = _auctionLocks.GetOrAdd(auctionId, _ => new System.Threading.SemaphoreSlim(1, 1));
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

                        if (auction == null) throw new AuctionOx.Exceptions.BusinessRuleException("Auction not found.");
                        
                        if (auction.Status != ItemStatus.Active) throw new AuctionOx.Exceptions.BusinessRuleException("Auction is not active.");
                        
                        if (auction.SellerId == userId) throw new AuctionOx.Exceptions.BusinessRuleException("Sellers cannot bid on their own auctions.");

                        // Auction must have started
                        if (DateTime.UtcNow < auction.StartTime) throw new AuctionOx.Exceptions.BusinessRuleException("This auction has not started yet.");
                        
                        if (DateTime.UtcNow > auction.EndTime) throw new AuctionOx.Exceptions.BusinessRuleException("Auction has already ended.");

                        if (request.BidAmount <= auction.CurrentPrice)
                            throw new AuctionOx.Exceptions.BusinessRuleException($"Bid must be higher than the current price of {auction.CurrentPrice:C}.");

                        bool isBuyItNow = auction.BuyItNowPrice.HasValue && request.BidAmount >= auction.BuyItNowPrice.Value;
                        if (isBuyItNow)
                        {
                            request.BidAmount = auction.BuyItNowPrice!.Value;
                        }

                        var bid = AuctionOx.Helpers.AuctionBiddingHelper.CreateWinningBidAndCloseOrExtend(auction, userId, request.BidAmount, isBuyItNow);

                        await _unitOfWork.Bids.AddAsync(bid);
                        _unitOfWork.Auctions.Update(auction);
                        await _unitOfWork.CompleteAsync();

                        var bidWithNav = await _unitOfWork.Bids.Query()
                            .Include(b => b.Bidder)
                            .FirstOrDefaultAsync(b => b.Id == bid.Id);

                        _logger.LogInformation("Bid of {Amount} successfully placed on auction {AuctionId} by user {UserId}.", bid.BidAmount, auctionId, userId);

                        return _mapper.Map<BidDto>(bidWithNav ?? bid);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (retryCount == maxRetries - 1)
                        {
                            throw new AuctionOx.Exceptions.BusinessRuleException("The auction was updated by another user. Please refresh and try again.");
                        }
                        if (_unitOfWork is AuctionOx.Repositories.Implementations.UnitOfWork uow && uow.Context != null)
                        {
                            uow.Context.ChangeTracker.Clear();
                        }
                    }
                }

                throw new AuctionOx.Exceptions.BusinessRuleException("Failed to place bid after multiple attempts.");
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
