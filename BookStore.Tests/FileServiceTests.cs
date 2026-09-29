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
    public class FileServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly string _testWebRootPath;

        public FileServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _testWebRootPath = Path.Combine(Path.GetTempPath(), "BookStoreTest_WebRoot_" + Guid.NewGuid());
            Directory.CreateDirectory(_testWebRootPath);
            Directory.CreateDirectory(Path.Combine(_testWebRootPath, "uploads"));

            _mockEnvironment.Setup(m => m.WebRootPath).Returns(_testWebRootPath);
        }

        [Fact]
        public async Task SaveFileAsync_PathTraversalInFolderName_ThrowsArgumentException()
        {
            // Arrange
            var fileService = new FileService(_mockEnvironment.Object);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                fileService.SaveFileAsync(mockFile.Object, "../secret_folder"));
        }

        [Fact]
        public async Task DeleteFileAsync_PathTraversal_ReturnsFalseAndDoesNotDeleteOutsideFile()
        {
            // Arrange
            var fileService = new FileService(_mockEnvironment.Object);

            // Create a file outside webRootPath
            var outsideDir = Path.Combine(Path.GetTempPath(), "BookStoreTest_Outside_" + Guid.NewGuid());
            Directory.CreateDirectory(outsideDir);
            var outsideFilePath = Path.Combine(outsideDir, "secret.txt");
            await File.WriteAllTextAsync(outsideFilePath, "sensitive content");

            try
            {
                // Relative path with traversal attempting to reach outsideFilePath
                var relativeTraversalPath = "../" + Path.GetFileName(outsideDir) + "/secret.txt";

                // Act
                var result = await fileService.DeleteFileAsync(relativeTraversalPath);

                // Assert
                Assert.False(result);
                Assert.True(File.Exists(outsideFilePath));
            }
            finally
            {
                if (Directory.Exists(_testWebRootPath)) Directory.Delete(_testWebRootPath, true);
                if (Directory.Exists(outsideDir)) Directory.Delete(outsideDir, true);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_ValidFileInWebRoot_DeletesFileAndReturnsTrue()
        {
            // Arrange
            var fileService = new FileService(_mockEnvironment.Object);
            var filePathInside = Path.Combine(_testWebRootPath, "uploads", "test_file.txt");
            await File.WriteAllTextAsync(filePathInside, "test");

            try
            {
                // Act
                var result = await fileService.DeleteFileAsync("uploads/test_file.txt");

                // Assert
                Assert.True(result);
                Assert.False(File.Exists(filePathInside));
            }
            finally
            {
                if (Directory.Exists(_testWebRootPath)) Directory.Delete(_testWebRootPath, true);
            }
        }
    }
}
