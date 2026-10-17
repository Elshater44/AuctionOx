using AuctionOx.DTOs.Auctions;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuctionOx.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuctionsController(IAuctionService auctionService, UserManager<ApplicationUser> userManager)
        {
            _auctionService = auctionService;
            _userManager = userManager;
        }

        private async Task<bool> IsUserValidAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId) != null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuctions(
            [FromQuery] int? categoryId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] string? sortBy,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _auctionService.GetAuctionsAsync(categoryId, status, search, sortBy, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuction(int id)
        {
            var result = await _auctionService.GetAuctionDetailsAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !await IsUserValidAsync(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var result = await _auctionService.CreateAuctionAsync(userId, request);
            return CreatedAtAction(nameof(GetAuction), new { id = result.Id }, result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuction(int id, [FromBody] UpdateAuctionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !await IsUserValidAsync(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            try
            {
                var isAdmin = User.IsInRole("Admin");
                var result = await _auctionService.UpdateAuctionAsync(id, userId, request, isAdmin);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !await IsUserValidAsync(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var deleted = await _auctionService.DeleteAuctionAsync(id, userId);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpGet("my-auctions")]
        public async Task<IActionResult> GetMyAuctions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !await IsUserValidAsync(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var result = await _auctionService.GetMyAuctionsAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id}/buy-now")]
        public async Task<IActionResult> BuyItNow(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !await IsUserValidAsync(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            try
            {
                var success = await _auctionService.BuyItNowAsync(id, userId);
                if (!success) return BadRequest(new { message = "Buy it now is not available for this auction." });
                return Ok(new { message = "Item successfully purchased." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> AdminDeleteAuction(int id)
        {
            var deleted = await _auctionService.AdminDeleteAuctionAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id}/suspend")]
        public async Task<IActionResult> AdminSuspendAuction(int id)
        {
            var suspended = await _auctionService.SuspendAuctionAsync(id);
            if (!suspended) return NotFound();
            return Ok(new { message = "Auction suspended successfully." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id}/resume")]
        public async Task<IActionResult> AdminResumeAuction(int id)
        {
            var resumed = await _auctionService.ResumeAuctionAsync(id);
            if (!resumed) return NotFound();
            return Ok(new { message = "Auction resumed successfully." });
        }
    }
}
