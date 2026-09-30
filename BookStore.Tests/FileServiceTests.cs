using System;
using System.IO;
using System.Threading.Tasks;
using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly string _tempWebRoot;

        public FileServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _tempWebRoot = Path.Combine(Path.GetTempPath(), "BookStoreTest_WebRoot_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRoot);
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(_tempWebRoot);
        }

        [Fact]
        public async Task DeleteFileAsync_PathTraversalAttempt_ReturnsFalseAndDoesNotDeleteFile()
        {
            // Arrange
            var outsideDir = Path.Combine(Path.GetTempPath(), "BookStoreTest_Outside_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outsideDir);
            var sensitiveFilePath = Path.Combine(outsideDir, "sensitive.txt");
            await File.WriteAllTextAsync(sensitiveFilePath, "sensitive data");

            var fileService = new FileService(_mockEnvironment.Object);

            // Act - attempt traversal to reach sensitiveFilePath outside _tempWebRoot
            var relativeTraversalPath = "../" + Path.GetFileName(outsideDir) + "/sensitive.txt";
            var result = await fileService.DeleteFileAsync(relativeTraversalPath);

            // Assert
            Assert.False(result);
            Assert.True(File.Exists(sensitiveFilePath), "File outside WebRootPath should not be deleted");

            // Cleanup
            Directory.Delete(outsideDir, true);
            Directory.Delete(_tempWebRoot, true);
        }

        [Fact]
        public async Task SaveFileAsync_PathTraversalInFolderName_ThrowsArgumentException()
        {
            // Arrange
            var fileService = new FileService(_mockEnvironment.Object);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => fileService.SaveFileAsync(mockFile.Object, "../../secret"));

            // Cleanup
            Directory.Delete(_tempWebRoot, true);
        }

        [Fact]
        public async Task DeleteFileAsync_ValidFileInWebRoot_DeletesFileAndReturnsTrue()
        {
            // Arrange
            var validFilePath = Path.Combine(_tempWebRoot, "uploads", "testfile.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(validFilePath)!);
            await File.WriteAllTextAsync(validFilePath, "dummy image content");

            var fileService = new FileService(_mockEnvironment.Object);

            // Act
            var result = await fileService.DeleteFileAsync("uploads/testfile.jpg");

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(validFilePath));

            // Cleanup
            if (Directory.Exists(_tempWebRoot)) Directory.Delete(_tempWebRoot, true);
        }
    }
}
