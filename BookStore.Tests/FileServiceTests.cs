using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly FileService _fileService;
        private readonly string _testWebRootPath;

        public FileServiceTests()
        {
            _testWebRootPath = Path.Combine(Path.GetTempPath(), "BookStoreTest_WebRoot_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testWebRootPath);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(env => env.WebRootPath).Returns(_testWebRootPath);

            _fileService = new FileService(_mockEnvironment.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testWebRootPath))
            {
                try
                {
                    Directory.Delete(_testWebRootPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldReturnFalse_WhenPathTraversalAttempted()
        {
            // Arrange: Create a secret file outside WebRootPath
            var parentDir = Directory.GetParent(_testWebRootPath)!.FullName;
            var secretFileName = "secret_" + Guid.NewGuid().ToString("N") + ".txt";
            var secretFilePath = Path.Combine(parentDir, secretFileName);
            await File.WriteAllTextAsync(secretFilePath, "sensitive data");

            try
            {
                // Path traversal attempt referencing the secret file outside webroot
                var maliciousPath = $"../{secretFileName}";

                // Act
                var result = await _fileService.DeleteFileAsync(maliciousPath);

                // Assert
                Assert.False(result);
                Assert.True(File.Exists(secretFilePath)); // File should NOT be deleted
            }
            finally
            {
                if (File.Exists(secretFilePath))
                {
                    File.Delete(secretFilePath);
                }
            }
        }

        [Fact]
        public async Task SaveFileAsync_ShouldThrowArgumentException_WhenPathTraversalAttemptedInFolderName()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.png");

            var maliciousFolderName = "../../outside";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.SaveFileAsync(mockFile.Object, maliciousFolderName));
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldSuccessfullyDeleteFile_WhenFileIsInsideWebRoot()
        {
            // Arrange
            var uploadsDir = Path.Combine(_testWebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var validFileName = "image_" + Guid.NewGuid().ToString("N") + ".jpg";
            var validFilePath = Path.Combine(uploadsDir, validFileName);
            await File.WriteAllTextAsync(validFilePath, "dummy content");

            var relativePath = $"uploads/{validFileName}";

            // Act
            var result = await _fileService.DeleteFileAsync(relativePath);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(validFilePath));
        }
    }
}
