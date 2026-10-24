using System.IO;
using System.Threading.Tasks;

namespace AuctionOx.Services.Interfaces
{
    public interface IImageStorageService
    {
        /// <summary>
        /// Uploads an image stream to storage and returns the unique persistent file_id.
        /// </summary>
        Task<string> UploadImageAsync(Stream stream, string fileName, string contentType);

        /// <summary>
        /// Retrieves the image stream and content type from storage using the file_id.
        /// </summary>
        Task<(Stream Stream, string ContentType)?> GetImageAsync(string fileId);
    }
}
