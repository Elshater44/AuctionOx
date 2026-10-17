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
            if (string.IsNullOrEmpty(userId) || await _userManager.FindByIdAsync(userId) == null)
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

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

        [Authorize]
        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || await _userManager.FindByIdAsync(userId) == null)
                return Unauthorized(new { message = "User session is invalid or user no longer exists. Please sign in again." });

            var bids = await _bidService.GetMyBidsAsync(userId);
            return Ok(bids);
        }
    }
}
