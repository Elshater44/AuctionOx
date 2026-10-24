using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuctionOx.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuctionOx.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageStorageService _storageService;

        public ImagesController(IImageStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Public image delivery reverse proxy (caches for 24 hours).
        /// </summary>
        [HttpGet("{fileId}")]
        [AllowAnonymous]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetImage(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { message = "FileId is required." });
            }

            var result = await _storageService.GetImageAsync(fileId);
            if (result == null)
            {
                return NotFound(new { message = "Image not found." });
            }

            return File(result.Value.Stream, result.Value.ContentType);
        }

        /// <summary>
        /// Upload endpoint for authenticated users.
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No image file was provided." });
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "Image size exceeds the maximum limit of 10MB." });
            }

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new { message = "Only JPEG, PNG, and WebP image formats are supported." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file extension." });
            }

            using var stream = file.OpenReadStream();
            var fileId = await _storageService.UploadImageAsync(stream, file.FileName, file.ContentType);

            var baseUrl = Request != null ? $"{Request.Scheme}://{Request.Host}" : string.Empty;
            var publicUrl = string.IsNullOrEmpty(baseUrl) ? $"/api/images/{fileId}" : $"{baseUrl}/api/images/{fileId}";
            return Ok(new
            {
                fileId,
                url = publicUrl
            });
        }
    }
}
