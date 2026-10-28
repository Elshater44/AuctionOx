using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;

namespace AuctionOx.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<UserProfileDto?> GetProfileAsync(string userId);
        Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
        Task<AdminUserDto> CreateAdminAsync(CreateAdminRequest request);
        Task<IEnumerable<AdminUserDto>> GetAdminsAsync();
        Task<AdminUserDto> UpdateAdminRoleAsync(string targetUserId, string newRole, string currentUserId);
        Task DeleteAdminAsync(string targetUserId, string currentUserId);
        Task<IEnumerable<UserManagementDto>> GetAllUsersAsync(string? roleFilter = null, string? search = null);
        Task<UserManagementDto> SuspendUserAsync(string targetUserId, int days, string currentUserId, string? reason = null);
        Task<UserManagementDto> BanUserAsync(string targetUserId, string currentUserId, string? reason = null);
        Task<UserManagementDto> ReactivateUserAsync(string targetUserId, string currentUserId);
        Task DeleteUserAccountAsync(string targetUserId, string currentUserId);
    }
}
