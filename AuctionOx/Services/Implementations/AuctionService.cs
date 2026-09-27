using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Data;
using AuctionOx.DTOs.Auctions;
using AuctionOx.DTOs.Bids;
using AuctionOx.DTOs.Common;
using AuctionOx.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AuctionOx.Services.Implementations
{
    public class AuctionService : Interfaces.IAuctionService
    {
        private readonly AuctionOx.Repositories.Interfaces.IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AuctionService(AuctionOx.Repositories.Interfaces.IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<AuctionItemDto>> GetAuctionsAsync(int? categoryId, string? status, string? search, string? sortBy, int pageNumber, int pageSize)
        {
            // Guard pagination bounds
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Auctions.Query()
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Bids)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ItemStatus>(status, true, out var parsedStatus))
                query = query.Where(a => a.Status == parsedStatus);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Title.Contains(search) || a.Description.Contains(search));

            query = sortBy?.ToLower() switch
            {
                "endtime" => query.OrderBy(a => a.EndTime),
                "endtime_desc" => query.OrderByDescending(a => a.EndTime),
                "price" => query.OrderBy(a => a.CurrentPrice),
                "price_desc" => query.OrderByDescending(a => a.CurrentPrice),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<AuctionItemDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = _mapper.Map<List<AuctionItemDto>>(items)
            };
        }

        public async Task<AuctionDetailDto?> GetAuctionDetailsAsync(int id)
        {
            var auction = await _unitOfWork.Auctions.Query()
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null) return null;

            var detailDto = _mapper.Map<AuctionDetailDto>(auction);
            
            // Map RecentBids manually since it's custom ordered
            detailDto.RecentBids = _mapper.Map<List<BidDto>>(
                auction.Bids.OrderByDescending(b => b.BidTime).Take(10)
            );

            return detailDto;
        }

        public async Task<AuctionItemDto> CreateAuctionAsync(string userId, CreateAuctionRequest request)
        {
            // --- Input Validation ---
            if (request.StartTime >= request.EndTime)
                throw new InvalidOperationException("Start time must be before end time.");

            if (request.BuyItNowPrice.HasValue && request.BuyItNowPrice.Value <= request.StartingPrice)
                throw new InvalidOperationException("Buy It Now price must be greater than the starting price.");

            if (request.AntiSnipingMinutes < 0)
                throw new InvalidOperationException("Anti-sniping minutes cannot be negative.");

            var categoryExists = await _unitOfWork.Categories.Query()
                .AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException("The specified category does not exist.");

            var auction = _mapper.Map<AuctionItem>(request);
            auction.SellerId = userId;
            auction.Status = ItemStatus.Active;

            await _unitOfWork.Auctions.AddAsync(auction);
            await _unitOfWork.CompleteAsync();

            // Load nav props for DTO mapping
            var auctionWithNav = await _unitOfWork.Auctions.Query()
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == auction.Id);

            return _mapper.Map<AuctionItemDto>(auctionWithNav);
        }

        public async Task<AuctionItemDto?> UpdateAuctionAsync(int id, string userId, UpdateAuctionRequest request)
        {
            var auction = await _unitOfWork.Auctions.Query()
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.Id == id && a.SellerId == userId);

            if (auction == null) return null;

            // Allow editing until the first bid is placed
            if (auction.Bids.Any())
                throw new InvalidOperationException("Auction cannot be updated after bids have been placed.");

            // Validate updated times
            if (request.StartTime >= request.EndTime)
                throw new InvalidOperationException("Start time must be before end time.");

            if (request.BuyItNowPrice.HasValue && request.BuyItNowPrice.Value <= auction.StartingPrice)
                throw new InvalidOperationException("Buy It Now price must be greater than the starting price.");

            if (request.CategoryId != auction.CategoryId)
            {
                var categoryExists = await _unitOfWork.Categories.Query()
                    .AnyAsync(c => c.Id == request.CategoryId);
                if (!categoryExists)
                    throw new InvalidOperationException("The specified category does not exist.");
            }

            _mapper.Map(request, auction);

            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();
            
            return _mapper.Map<AuctionItemDto>(auction);
        }

        public async Task<bool> DeleteAuctionAsync(int id, string userId)
        {
            var auction = await _unitOfWork.Auctions.Query().FirstOrDefaultAsync(a => a.Id == id && a.SellerId == userId);
            if (auction == null) return false;

            auction.IsDeleted = true;
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();
            
            return true;
        }

        public async Task<PagedResult<AuctionItemDto>> GetMyAuctionsAsync(string userId, int pageNumber, int pageSize)
        {
            // Guard pagination bounds
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Auctions.Query()
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Bids)
                .Where(a => a.SellerId == userId)
                .OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<AuctionItemDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = _mapper.Map<List<AuctionItemDto>>(items)
            };
        }

        public async Task<bool> BuyItNowAsync(int id, string userId)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            
            if (auction == null || auction.Status != ItemStatus.Active || auction.BuyItNowPrice == null)
                return false;

            // Cannot buy an expired auction
            if (DateTime.UtcNow > auction.EndTime)
                throw new InvalidOperationException("This auction has already ended.");

            // Cannot buy if competitive bids have already exceeded the buy-it-now price
            if (auction.CurrentPrice >= auction.BuyItNowPrice.Value)
                throw new InvalidOperationException("Current bid price already meets or exceeds the Buy It Now price.");

            if (auction.SellerId == userId)
                throw new InvalidOperationException("You cannot buy your own auction.");

            // Create winning bid
            var bid = new Bid
            {
                AuctionItemId = id,
                BidAmount = auction.BuyItNowPrice.Value,
                BidTime = DateTime.UtcNow,
                BidderId = userId,
                IsWinningBid = true
            };

            auction.CurrentPrice = bid.BidAmount;
            auction.Status = ItemStatus.Completed;
            auction.EndTime = DateTime.UtcNow;

            await _unitOfWork.Bids.AddAsync(bid);
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();

            return true;
        }
    }
}
