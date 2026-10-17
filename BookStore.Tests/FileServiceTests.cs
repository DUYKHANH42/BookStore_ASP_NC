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
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly FileService _fileService;
        private readonly string _tempWebRoot;

        public FileServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _tempWebRoot = Path.Combine(Path.GetTempPath(), "BookStoreTest_WebRoot_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRoot);
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

            _fileService = new FileService(_mockEnvironment.Object);
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldDeleteFile_WhenPathIsInsideWebRoot()
        {
            // Arrange
            var relativeFilePath = Path.Combine("uploads", "testfile.txt");
            var fullPath = Path.Combine(_tempWebRoot, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, "test content");

            Assert.True(File.Exists(fullPath));

            // Act
            var result = await _fileService.DeleteFileAsync(relativeFilePath);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldPreventPathTraversal_AndNotDeleteOutsideFile()
        {
            // Arrange
            var outsideDir = Path.Combine(Path.GetTempPath(), "Outside_Dir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outsideDir);
            var sensitiveFilePath = Path.Combine(outsideDir, "secret.txt");
            await File.WriteAllTextAsync(sensitiveFilePath, "sensitive content");

            Assert.True(File.Exists(sensitiveFilePath));

            // Malicious relative path trying to traverse up out of _tempWebRoot
            var traversalPath = Path.Combine("..", Path.GetFileName(outsideDir), "secret.txt");

            // Act
            var result = await _fileService.DeleteFileAsync(traversalPath);

            // Assert
            Assert.False(result);
            Assert.True(File.Exists(sensitiveFilePath)); // File must NOT be deleted

            // Clean up outside file
            if (File.Exists(sensitiveFilePath)) File.Delete(sensitiveFilePath);
            if (Directory.Exists(outsideDir)) Directory.Delete(outsideDir);
        }
    }
}
