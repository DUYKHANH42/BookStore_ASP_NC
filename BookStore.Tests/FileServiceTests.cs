using BookStore.API.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class FileServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            string testWebRootPath = Path.Combine(Path.GetTempPath(), "BookStoreTestWebRoot");
            Directory.CreateDirectory(testWebRootPath);
            _mockEnvironment.Setup(e => e.WebRootPath).Returns(testWebRootPath);

            _fileService = new FileService(_mockEnvironment.Object);
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldReturnFalse_WhenPathTraversalAttempted()
        {
            // Arrange
            string pathTraversalInput = "../../appsettings.json";

            // Act
            bool result = await _fileService.DeleteFileAsync(pathTraversalInput);

            // Assert
            Assert.False(result);
        }
    }
}
