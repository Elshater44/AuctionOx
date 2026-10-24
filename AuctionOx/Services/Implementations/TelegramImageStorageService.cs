using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AuctionOx.Exceptions;
using AuctionOx.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuctionOx.Services.Implementations
{
    public class TelegramImageStorageService : IImageStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly string _channelId;
        private readonly ILogger<TelegramImageStorageService> _logger;

        public TelegramImageStorageService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TelegramImageStorageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _botToken = configuration["TelegramStorage:BotToken"] ?? string.Empty;
            _channelId = configuration["TelegramStorage:ChannelId"] ?? string.Empty;
        }

        public async Task<string> UploadImageAsync(Stream stream, string fileName, string contentType)
        {
            if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_channelId))
            {
                throw new BusinessRuleException("Telegram storage credentials (BotToken or ChannelId) are not configured.");
            }

            var url = $"https://api.telegram.org/bot{_botToken}/sendPhoto";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_channelId), "chat_id");

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "photo", fileName);

            var response = await _httpClient.PostAsync(url, form);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Telegram image upload failed. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseString);
                throw new BusinessRuleException("Failed to upload image to Telegram storage.");
            }

            using var doc = JsonDocument.Parse(responseString);
            if (!doc.RootElement.TryGetProperty("result", out var resultElement) ||
                !resultElement.TryGetProperty("photo", out var photoArray))
            {
                _logger.LogError("Unexpected response from Telegram: {Response}", responseString);
                throw new BusinessRuleException("Failed to parse Telegram image upload response.");
            }

            // The last item in photo array has the highest resolution
            var highestResPhoto = photoArray.EnumerateArray().Last();
            var fileId = highestResPhoto.GetProperty("file_id").GetString();

            if (string.IsNullOrEmpty(fileId))
            {
                throw new BusinessRuleException("Telegram did not return a valid file_id.");
            }

            _logger.LogInformation("Image {FileName} uploaded to Telegram channel {ChannelId} with FileId {FileId}", fileName, _channelId, fileId);
            return fileId;
        }

        public async Task<(Stream Stream, string ContentType)?> GetImageAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(_botToken))
            {
                _logger.LogError("Telegram BotToken is not configured.");
                return null;
            }

            // 1. Resolve temporary file_path
            var getFileUrl = $"https://api.telegram.org/bot{_botToken}/getFile?file_id={fileId}";
            var fileResponse = await _httpClient.GetAsync(getFileUrl);

            if (!fileResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram getFile returned HTTP {StatusCode} for FileId {FileId}", fileResponse.StatusCode, fileId);
                return null;
            }

            var fileJson = await fileResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(fileJson);

            if (!doc.RootElement.GetProperty("ok").GetBoolean() ||
                !doc.RootElement.GetProperty("result").TryGetProperty("file_path", out var filePathElement))
            {
                _logger.LogWarning("Telegram getFile failed or missing file_path for FileId {FileId}: {Response}", fileId, fileJson);
                return null;
            }

            var filePath = filePathElement.GetString();
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            // 2. Download raw image bytes from Telegram
            var downloadUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";
            var imageResponse = await _httpClient.GetAsync(downloadUrl);

            if (!imageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram file download returned HTTP {StatusCode} for path {FilePath}", imageResponse.StatusCode, filePath);
                return null;
            }

            var memoryStream = new MemoryStream();
            await imageResponse.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            return (memoryStream, contentType);
        }
    }
}
