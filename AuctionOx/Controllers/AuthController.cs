using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.DTOs.Auth;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

namespace AuctionOx.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AuthLimiter")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null) return NotFound();

            return Ok(profile);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("create-admin")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            try
            {
                var response = await _authService.CreateAdminAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admins")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _authService.GetAdminsAsync();
            return Ok(admins);
        }

        [HttpPut("admins/{id}/role")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> UpdateAdminRole(string id, [FromBody] UpdateAdminRoleRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var response = await _authService.UpdateAdminRoleAsync(id, request.Role, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("admins/{id}")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                await _authService.DeleteAdminAsync(id, currentUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region User Moderation & Account Management (Admin Protected)

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role, [FromQuery] string? search)
        {
            var users = await _authService.GetAllUsersAsync(role, search);
            return Ok(users);
        }

        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateAdminRoleRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var response = await _authService.UpdateAdminRoleAsync(id, request.Role, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/suspend")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> SuspendUser(string id, [FromBody] SuspendUserRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var response = await _authService.SuspendUserAsync(id, request.Days, currentUserId, request.Reason);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/ban")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> BanUser(string id, [FromBody] SuspendUserRequest? request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var response = await _authService.BanUserAsync(id, currentUserId, request?.Reason);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/reactivate")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> ReactivateUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var response = await _authService.ReactivateUserAsync(id, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                await _authService.DeleteUserAccountAsync(id, currentUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }
}
