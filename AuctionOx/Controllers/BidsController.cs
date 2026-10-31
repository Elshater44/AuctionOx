using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.DTOs.Bids;
using AuctionOx.Models;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuctionOx.Controllers
{
    [ApiController]
    [Route("api/auctions/{auctionId}/bids")]
    [EnableRateLimiting("BidLimiter")]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BidsController(IBidService bidService, UserManager<ApplicationUser> userManager)
        {
            _bidService = bidService;
            _userManager = userManager;
        }

        private async Task<(bool IsValid, string? ErrorMessage)> ValidateUserStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return (false, "User session is invalid or user no longer exists. Please sign in again.");

            if (await _userManager.IsLockedOutAsync(user))
                return (false, "Your account has been suspended or banned. Please contact administration.");

            return (true, null);
        }

        [HttpGet]
        public async Task<IActionResult> GetBids(int auctionId)
        {
            var bids = await _bidService.GetBidsForAuctionAsync(auctionId);
            return Ok(bids);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceBid(int auctionId, [FromBody] PlaceBidRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var (isValid, errorMessage) = await ValidateUserStatusAsync(userId);
            if (!isValid)
                return Unauthorized(new { message = errorMessage });

            try
            {
                var bid = await _bidService.PlaceBidAsync(auctionId, userId, request);
                return CreatedAtAction(nameof(GetBids), new { auctionId = auctionId }, bid);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    [ApiController]
    [Route("api/bids")]
    public class MyBidsController : ControllerBase
    {
        private readonly IBidService _bidService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyBidsController(IBidService bidService, UserManager<ApplicationUser> userManager)
        {
            _bidService = bidService;
            _userManager = userManager;
        }

        private async Task<(bool IsValid, string? ErrorMessage)> ValidateUserStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return (false, "User session is invalid or user no longer exists. Please sign in again.");

            if (await _userManager.IsLockedOutAsync(user))
                return (false, "Your account has been suspended or banned. Please contact administration.");

            return (true, null);
        }

        [Authorize]
        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var (isValid, errorMessage) = await ValidateUserStatusAsync(userId);
            if (!isValid)
                return Unauthorized(new { message = errorMessage });

            var bids = await _bidService.GetMyBidsAsync(userId);
            return Ok(bids);
        }
    }
}
