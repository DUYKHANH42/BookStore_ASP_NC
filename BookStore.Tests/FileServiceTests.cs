using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly string _tempWebRootPath;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _tempWebRootPath = Path.Combine(Path.GetTempPath(), "FileServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRootPath);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRootPath);

            _fileService = new FileService(_mockEnv.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempWebRootPath))
            {
                try
                {
                    Directory.Delete(_tempWebRootPath, true);
                }
                catch { }
            }
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName, string content = "dummy image data")
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns((Stream target, CancellationToken token) => ms.CopyToAsync(target, token));

            return fileMock;
        }

        [Theory]
        [InlineData("malicious.exe")]
        [InlineData("script.js")]
        [InlineData("payload.php")]
        [InlineData("page.html")]
        [InlineData("graphic.svg")]
        public async Task SaveFileAsync_DisallowedExtension_ThrowsArgumentException(string fileName)
        {
            var fileMock = CreateMockFormFile(fileName);

            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.SaveFileAsync(fileMock.Object, "avatars"));
        }

        [Theory]
        [InlineData("avatar.jpg")]
        [InlineData("photo.JPEG")]
        [InlineData("image.png")]
        [InlineData("banner.webp")]
        public async Task SaveFileAsync_ValidExtension_Succeeds(string fileName)
        {
            var fileMock = CreateMockFormFile(fileName);

            var result = await _fileService.SaveFileAsync(fileMock.Object, "products");

            Assert.False(string.IsNullOrEmpty(result));
            Assert.EndsWith(Path.GetExtension(fileName).ToLowerInvariant(), result);
        }

        [Fact]
        [Trait("Category", "Security")]
        public async Task SaveFileAsync_PathTraversalFolder_ThrowsArgumentException()
        {
            var fileMock = CreateMockFormFile("test.jpg");

            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.SaveFileAsync(fileMock.Object, "../../outside"));
        }

        [Fact]
        [Trait("Category", "Security")]
        public async Task DeleteFileAsync_PathTraversalAttempt_ReturnsFalse()
        {
            // Create a sensitive file outside WebRootPath
            var parentDir = Directory.GetParent(_tempWebRootPath)!.FullName;
            var sensitiveFile = Path.Combine(parentDir, "sensitive_" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(sensitiveFile, "secret");

            try
            {
                var traversalPath = "../" + Path.GetFileName(sensitiveFile);
                var result = await _fileService.DeleteFileAsync(traversalPath);

                Assert.False(result);
                Assert.True(File.Exists(sensitiveFile), "Sensitive file outside WebRootPath must not be deleted!");
            }
            finally
            {
                if (File.Exists(sensitiveFile))
                {
                    File.Delete(sensitiveFile);
                }
            }
        }

        [Fact]
        public async Task DeleteFileAsync_ValidFileInWebRoot_DeletesFile()
        {
            var uploadsDir = Path.Combine(_tempWebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var filePath = Path.Combine(uploadsDir, "test.jpg");
            File.WriteAllText(filePath, "dummy");

            var result = await _fileService.DeleteFileAsync("uploads/test.jpg");

            Assert.True(result);
            Assert.False(File.Exists(filePath));
        }
    }
}
