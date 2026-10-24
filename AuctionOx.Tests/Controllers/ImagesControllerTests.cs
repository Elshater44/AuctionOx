using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AuctionOx.Controllers;
using AuctionOx.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuctionOx.Tests.Controllers
{
    public class ImagesControllerTests
    {
        private readonly Mock<IImageStorageService> _mockStorageService;
        private readonly ImagesController _controller;

        public ImagesControllerTests()
        {
            _mockStorageService = new Mock<IImageStorageService>();
            _controller = new ImagesController(_mockStorageService.Object);
        }

        #region Attribute & Security Tests

        [Fact]
        public void ImagesController_HasExpectedAttributes()
        {
            var type = typeof(ImagesController);

            type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
            var route = type.GetCustomAttribute<RouteAttribute>();
            route.Should().NotBeNull();
            route!.Template.Should().Be("api/[controller]");
        }

        [Fact]
        public void UploadImage_RequiresAuthorization()
        {
            var method = typeof(ImagesController).GetMethod(nameof(ImagesController.UploadImage));
            method.Should().NotBeNull();

            var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
            authAttr.Should().NotBeNull("UploadImage must be protected with [Authorize]");
        }

        [Fact]
        public void GetImage_AllowsAnonymous()
        {
            var method = typeof(ImagesController).GetMethod(nameof(ImagesController.GetImage));
            method.Should().NotBeNull();

            var allowAnon = method!.GetCustomAttribute<AllowAnonymousAttribute>();
            allowAnon.Should().NotBeNull("GetImage must allow anonymous access for public image viewing");
        }

        #endregion

        #region GetImage Action Tests

        [Fact]
        public async Task GetImage_WhenFileExists_ReturnsFileStreamResult()
        {
            var fakeStream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));
            _mockStorageService
                .Setup(s => s.GetImageAsync("file_123"))
                .ReturnsAsync((fakeStream, "image/jpeg"));

            var result = await _controller.GetImage("file_123");

            var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
            fileResult.ContentType.Should().Be("image/jpeg");
        }

        [Fact]
        public async Task GetImage_WhenNotFound_ReturnsNotFound()
        {
            _mockStorageService
                .Setup(s => s.GetImageAsync("missing_file"))
                .ReturnsAsync(((Stream, string)?)null);

            var result = await _controller.GetImage("missing_file");

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetImage_WhenFileIdIsEmpty_ReturnsBadRequest(string fileId)
        {
            var result = await _controller.GetImage(fileId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UploadImage Action Tests

        [Fact]
        public async Task UploadImage_WhenFileIsNull_ReturnsBadRequest()
        {
            var result = await _controller.UploadImage(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadImage_WhenFileIsEmpty_ReturnsBadRequest()
        {
            var emptyFile = new Mock<IFormFile>();
            emptyFile.Setup(f => f.Length).Returns(0);

            var result = await _controller.UploadImage(emptyFile.Object);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadImage_WhenFileExceeds5MB_ReturnsBadRequest()
        {
            var largeFile = new Mock<IFormFile>();
            largeFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB

            var result = await _controller.UploadImage(largeFile.Object);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadImage_WhenInvalidContentType_ReturnsBadRequest()
        {
            var exeFile = new Mock<IFormFile>();
            exeFile.Setup(f => f.Length).Returns(1024);
            exeFile.Setup(f => f.ContentType).Returns("application/x-msdownload");
            exeFile.Setup(f => f.FileName).Returns("malware.exe");

            var result = await _controller.UploadImage(exeFile.Object);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadImage_WhenSpoofedMagicBytes_ReturnsBadRequest()
        {
            // Named .jpg but containing plain text instead of JPEG headers
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("THIS_IS_NOT_A_REAL_IMAGE"));
            var spoofedFile = new Mock<IFormFile>();
            spoofedFile.Setup(f => f.Length).Returns(1024);
            spoofedFile.Setup(f => f.ContentType).Returns("image/jpeg");
            spoofedFile.Setup(f => f.FileName).Returns("fake.jpg");
            spoofedFile.Setup(f => f.OpenReadStream()).Returns(stream);

            var result = await _controller.UploadImage(spoofedFile.Object);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadImage_WhenValidJpeg_UploadsAndReturnsFileIdAndUrl()
        {
            // Valid JPEG magic header: FF D8 FF E0
            var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
            var stream = new MemoryStream(jpegBytes);
            var validFile = new Mock<IFormFile>();
            validFile.Setup(f => f.Length).Returns(jpegBytes.Length);
            validFile.Setup(f => f.ContentType).Returns("image/jpeg");
            validFile.Setup(f => f.FileName).Returns("photo.jpg");
            validFile.Setup(f => f.OpenReadStream()).Returns(stream);

            _mockStorageService
                .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), "photo.jpg", "image/jpeg"))
                .ReturnsAsync("tg_file_id_999");

            var result = await _controller.UploadImage(validFile.Object);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadImage_WhenValidPng_UploadsAndReturnsFileIdAndUrl()
        {
            // Valid PNG magic header: 89 50 4E 47 0D 0A 1A 0A
            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
            var stream = new MemoryStream(pngBytes);
            var validFile = new Mock<IFormFile>();
            validFile.Setup(f => f.Length).Returns(pngBytes.Length);
            validFile.Setup(f => f.ContentType).Returns("image/png");
            validFile.Setup(f => f.FileName).Returns("graphic.png");
            validFile.Setup(f => f.OpenReadStream()).Returns(stream);

            _mockStorageService
                .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), "graphic.png", "image/png"))
                .ReturnsAsync("tg_file_id_png");

            var result = await _controller.UploadImage(validFile.Object);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        #endregion
    }
}
