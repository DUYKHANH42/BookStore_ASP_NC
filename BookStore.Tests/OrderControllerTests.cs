using BookStore.API.Areas.Customer.Controllers;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace BookStore.Tests
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task VNPayCallback_ShouldRedirectToConfiguredClientUrl()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "AppSettings:ClientUrl", "https://mystore.example.com" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var orderServiceMock = new Mock<OrderService>(null!, null!, null!, null!, null!);
            var controller = new OrderController(orderServiceMock.Object, configuration);

            // Act
            var result = await controller.VNPayCallback();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("https://mystore.example.com/profile", redirectResult.Url);
        }

        [Fact]
        public async Task VNPayCallback_ShouldFallbackToDefaultUrl_WhenConfigMissing()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var orderServiceMock = new Mock<OrderService>(null!, null!, null!, null!, null!);
            var controller = new OrderController(orderServiceMock.Object, configuration);

            // Act
            var result = await controller.VNPayCallback();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("http://localhost:4200/profile", redirectResult.Url);
        }
    }
}
