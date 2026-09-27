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

        public BidService(AuctionOx.Repositories.Interfaces.IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<BidDto> PlaceBidAsync(int auctionId, string userId, PlaceBidRequest request)
        {
            int maxRetries = 3;
            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    var auction = await _unitOfWork.Auctions.Query()
                        .Include(a => a.Bids)
                        .FirstOrDefaultAsync(a => a.Id == auctionId);

                    if (auction == null) throw new Exception("Auction not found.");
                    
                    if (auction.Status != ItemStatus.Active) throw new Exception("Auction is not active.");
                    
                    if (auction.SellerId == userId) throw new Exception("Sellers cannot bid on their own auctions.");

                    // Auction must have started
                    if (DateTime.UtcNow < auction.StartTime) throw new Exception("This auction has not started yet.");
                    
                    if (DateTime.UtcNow > auction.EndTime) throw new Exception("Auction has already ended.");

                    if (request.BidAmount <= auction.CurrentPrice)
                        throw new Exception($"Bid must be higher than the current price of {auction.CurrentPrice:C}.");

                    if (auction.BuyItNowPrice.HasValue && request.BidAmount >= auction.BuyItNowPrice.Value)
                    {
                        // Force it to BuyItNowPrice if they overbid BuyItNow
                        request.BidAmount = auction.BuyItNowPrice.Value;
                        auction.Status = ItemStatus.Completed;
                        auction.EndTime = DateTime.UtcNow;
                    }

                    // Anti-Sniping Logic: reset EndTime to now + window (not cumulative)
                    if (auction.Status == ItemStatus.Active)
                    {
                        var antiSnipingWindow = TimeSpan.FromMinutes(auction.AntiSnipingMinutes);
                        var timeRemaining = auction.EndTime - DateTime.UtcNow;
                        if (timeRemaining < antiSnipingWindow)
                        {
                            auction.EndTime = DateTime.UtcNow.Add(antiSnipingWindow);
                        }
                    }

                    // Unmark previous winning bids
                    foreach (var existingBid in auction.Bids.Where(b => b.IsWinningBid))
                    {
                        existingBid.IsWinningBid = false;
                        _unitOfWork.Bids.Update(existingBid);
                    }

                    var bid = new Bid
                    {
                        AuctionItemId = auctionId,
                        BidderId = userId,
                        BidAmount = request.BidAmount,
                        BidTime = DateTime.UtcNow,
                        IsWinningBid = true
                    };

                    auction.CurrentPrice = bid.BidAmount;
                    
                    await _unitOfWork.Bids.AddAsync(bid);
                    _unitOfWork.Auctions.Update(auction);
                    await _unitOfWork.CompleteAsync();

                    var bidWithNav = await _unitOfWork.Bids.Query()
                        .Include(b => b.Bidder)
                        .FirstOrDefaultAsync(b => b.Id == bid.Id);

                    return _mapper.Map<BidDto>(bidWithNav ?? bid);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (retryCount == maxRetries - 1)
                    {
                        throw new Exception("The auction was updated by another user. Please refresh and try again.");
                    }
                    // Detach all entries so the next attempt pulls fresh data
                    // Since we use scoped unit of work, we can just let the loop continue and query again,
                    // but we must clear EF's change tracker tracking
                    if (_unitOfWork is AuctionOx.Repositories.Implementations.UnitOfWork uow && uow.Context != null)
                    {
                        uow.Context.ChangeTracker.Clear();
                    }
                }
            }

            throw new Exception("Failed to place bid after multiple attempts.");
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
