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
            var auction = await _unitOfWork.Auctions.Query()
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null) throw new Exception("Auction not found.");
            
            if (auction.Status != ItemStatus.Active) throw new Exception("Auction is not active.");
            
            if (auction.SellerId == userId) throw new Exception("Sellers cannot bid on their own auctions.");
            
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

            // Anti-Sniping Logic
            var timeRemaining = auction.EndTime - DateTime.UtcNow;
            if (timeRemaining.TotalMinutes < auction.AntiSnipingMinutes && auction.Status == ItemStatus.Active)
            {
                auction.EndTime = auction.EndTime.AddMinutes(auction.AntiSnipingMinutes);
            }

            // Unmark previous winning bid (optional logic, since only the final one at EndTime is truly winning)
            // But we'll mark this new bid as the tentative winning bid
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
