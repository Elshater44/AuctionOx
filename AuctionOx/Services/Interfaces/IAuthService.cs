using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;

namespace AuctionOx.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<UserProfileDto?> GetProfileAsync(string userId);
    }
}
