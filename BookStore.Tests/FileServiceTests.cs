using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests
    {
        [Fact]
        public async Task DeleteFileAsync_ShouldDeleteFile_WhenFileIsInWebRoot()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var testFilePath = Path.Combine(tempDir, "test.txt");
            await File.WriteAllTextAsync(testFilePath, "test content");

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(tempDir);

            var fileService = new FileService(mockEnv.Object);

            try
            {
                // Act
                var result = await fileService.DeleteFileAsync("test.txt");

                // Assert
                Assert.True(result);
                Assert.False(File.Exists(testFilePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldPreventPathTraversal_WhenAttemptingOutsideWebRoot()
        {
            // Arrange
            var rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var webRootDir = Path.Combine(rootDir, "wwwroot");
            Directory.CreateDirectory(webRootDir);

            var sensitiveFilePath = Path.Combine(rootDir, "sensitive.txt");
            await File.WriteAllTextAsync(sensitiveFilePath, "sensitive content");

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(webRootDir);

            var fileService = new FileService(mockEnv.Object);

            try
            {
                // Act - Attempt path traversal
                var result = await fileService.DeleteFileAsync("../sensitive.txt");

                // Assert
                Assert.False(result);
                Assert.True(File.Exists(sensitiveFilePath));
            }
            finally
            {
                if (Directory.Exists(rootDir))
                {
                    Directory.Delete(rootDir, true);
                }
            }
        }
    }
}
