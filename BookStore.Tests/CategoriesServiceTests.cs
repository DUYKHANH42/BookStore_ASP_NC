using BookStore.Application.DTO;
using BookStore.Application.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class CategoriesServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly CategoriesService _service;

        public CategoriesServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCache = new Mock<IMemoryCache>();
            
            object? cacheEntry = null;
            _mockCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
            _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

            _service = new CategoriesService(_mockUnitOfWork.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnListOfCategories_WhenCalled()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Sách Kinh Tế" },
                new Category { Id = 2, Name = "Sách Văn Học" }
            };
            _mockUnitOfWork.Setup(u => u.Categories.GetAllWithSubCategoriesAsync()).ReturnsAsync(categories);

            // Act
            var result = await _service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Sách Kinh Tế", result.First().Name);
        }

        [Fact]
        public async Task GetById_ShouldReturnCategory_WhenIdExists()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Sách Kinh Tế" };
            _mockUnitOfWork.Setup(u => u.Categories.GetByIdAsync(1)).ReturnsAsync(category);

            // Act
            var result = await _service.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Sách Kinh Tế", result.Name);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnTrue_WhenSuccess()
        {
            // Arrange
            var dto = new CategoryDTO { Name = "Sách Mới" };
            _mockUnitOfWork.Setup(u => u.Categories.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.Categories.AddAsync(It.IsAny<Category>()), Times.Once);
            _mockCache.Verify(m => m.Remove(It.IsAny<object>()), Times.Once); // Kiểm tra xem có xóa cache không
        }
    }
}
