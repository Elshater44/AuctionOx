using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AuctionOx.DTOs.Bids;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionOx.Controllers
{
    [ApiController]
    [Route("api/auctions/{auctionId}/bids")]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;

        public BidsController(IBidService bidService)
        {
            _bidService = bidService;
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
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

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

        public MyBidsController(IBidService bidService)
        {
            _bidService = bidService;
        }

        [Authorize]
        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var bids = await _bidService.GetMyBidsAsync(userId);
            return Ok(bids);
        }
    }
}
