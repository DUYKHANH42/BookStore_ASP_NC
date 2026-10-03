using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly FileService _fileService;
        private readonly string _tempDirectory;

        public FileServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "BookStoreTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(Path.Combine(_tempDirectory, "uploads"));

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(_tempDirectory);

            _fileService = new FileService(_mockEnvironment.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData("malicious.exe")]
        [InlineData("script.cshtml")]
        [InlineData("shell.php")]
        [InlineData("test.dll")]
        public async Task SaveFileAsync_InvalidExtension_ReturnsEmpty(string fileName)
        {
            // Arrange
            var file = CreateMockFormFile(fileName, "malicious content");

            // Act
            var result = await _fileService.SaveFileAsync(file, "products");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task SaveFileAsync_ValidExtension_SavesFileAndReturnsName()
        {
            // Arrange
            var file = CreateMockFormFile("sample.jpg", "image data");

            // Act
            var result = await _fileService.SaveFileAsync(file, "products");

            // Assert
            Assert.NotEmpty(result);
            Assert.EndsWith(".jpg", result);
            Assert.True(File.Exists(Path.Combine(_tempDirectory, "uploads", "products", result)));
        }

        [Theory]
        [InlineData("../")]
        [InlineData(@"..\..\")]
        [InlineData("../../secret")]
        public async Task SaveFileAsync_PathTraversal_PreventsDirectoryTraversal(string folderName)
        {
            // Arrange
            var file = CreateMockFormFile("sample.png", "image data");

            // Act
            var result = await _fileService.SaveFileAsync(file, folderName);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("../appsettings.json")]
        [InlineData(@"..\..\Windows\System32\cmd.exe")]
        public async Task DeleteFileAsync_PathTraversal_ReturnsFalse(string filePath)
        {
            // Act
            var result = await _fileService.DeleteFileAsync(filePath);

            // Assert
            Assert.False(result);
        }

        private static IFormFile CreateMockFormFile(string fileName, string content)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }
    }
}
