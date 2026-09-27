using System.Threading.Tasks;
using AuctionOx.DTOs.Auctions;
using AuctionOx.DTOs.Common;

namespace AuctionOx.Services.Interfaces
{
    public interface IAuctionService
    {
        Task<PagedResult<AuctionItemDto>> GetAuctionsAsync(int? categoryId, string? status, string? search, string? sortBy, int pageNumber, int pageSize);
        Task<AuctionDetailDto?> GetAuctionDetailsAsync(int id);
        Task<AuctionItemDto> CreateAuctionAsync(string userId, CreateAuctionRequest request);
        Task<AuctionItemDto?> UpdateAuctionAsync(int id, string userId, UpdateAuctionRequest request);
        Task<bool> DeleteAuctionAsync(int id, string userId);
        Task<bool> AdminDeleteAuctionAsync(int id);
        Task<bool> SuspendAuctionAsync(int id);
        Task<PagedResult<AuctionItemDto>> GetMyAuctionsAsync(string userId, int pageNumber, int pageSize);
        Task<bool> BuyItNowAsync(int id, string userId);
    }
}
