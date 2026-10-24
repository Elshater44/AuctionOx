using System;
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
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB limit
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

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
        /// Secure upload endpoint for authenticated users.
        /// Enforces 5 MB size limit, allowed MIME types, allowed extensions, and binary magic bytes validation.
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No image file was provided." });
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(new { message = "Image size exceeds the maximum limit of 5 MB." });
            }

            var contentType = file.ContentType.ToLowerInvariant();
            if (!AllowedMimeTypes.Contains(contentType))
            {
                return BadRequest(new { message = "Only JPEG, PNG, and WebP image formats are supported." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file extension. Only .jpg, .jpeg, .png, and .webp are allowed." });
            }

            using var stream = file.OpenReadStream();

            // Validate binary magic bytes to prevent spoofed/disguised files
            if (!IsValidImageHeader(stream, extension))
            {
                return BadRequest(new { message = "The uploaded file content is corrupted or not a valid image format." });
            }

            var fileId = await _storageService.UploadImageAsync(stream, file.FileName, contentType);

            var baseUrl = Request != null ? $"{Request.Scheme}://{Request.Host}" : string.Empty;
            var publicUrl = string.IsNullOrEmpty(baseUrl) ? $"/api/images/{fileId}" : $"{baseUrl}/api/images/{fileId}";
            return Ok(new
            {
                fileId,
                url = publicUrl
            });
        }

        /// <summary>
        /// Inspects the file stream's initial bytes to verify genuine JPEG, PNG, or WebP binary signatures.
        /// </summary>
        private static bool IsValidImageHeader(Stream stream, string extension)
        {
            if (!stream.CanSeek)
            {
                return true; // Fallback if non-seekable
            }

            stream.Position = 0;
            Span<byte> header = stackalloc byte[12];
            int bytesRead = stream.Read(header);
            stream.Position = 0; // Reset position for downstream consumers

            if (bytesRead < 4)
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 &&
                          header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                          header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
                ".webp" => bytesRead >= 12 &&
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && // "RIFF"
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50, // "WEBP"
                _ => false
            };
        }
    }
}
