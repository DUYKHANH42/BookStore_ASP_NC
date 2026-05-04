using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IZaloPayService> _mockZaloPay;
        private readonly Mock<IPayOSService> _mockPayOS;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly Mock<IFlashSaleRepository> _mockFlashSales;
        private readonly Mock<ICartRepository> _mockCartRepo;
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<IVnPayService> _mockVnPay; 
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockZaloPay = new Mock<IZaloPayService>();
            _mockPayOS = new Mock<IPayOSService>();
            _mockVnPay = new Mock<IVnPayService>();
            _mockNotification = new Mock<INotificationService>();
            _mockFlashSales = new Mock<IFlashSaleRepository>();
            _mockCartRepo = new Mock<ICartRepository>();
            _mockOrderRepo = new Mock<IOrderRepository>();

            // Setup UOW to return mocked repositories
            _mockUnitOfWork.Setup(u => u.FlashSales).Returns(_mockFlashSales.Object);
            _mockUnitOfWork.Setup(u => u.Carts).Returns(_mockCartRepo.Object);
            _mockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepo.Object);

            _orderService = new OrderService(
                _mockUnitOfWork.Object,
                _mockZaloPay.Object,
                _mockPayOS.Object,
                _mockVnPay.Object,
                _mockNotification.Object);
        }

        [Fact]
        public async Task PlaceOrderAsync_ShouldReturnNull_WhenCartIsEmpty()
        {
            // Arrange
            string userId = "user-123";
            _mockCartRepo.Setup(u => u.GetCartByUserIdAsync(userId))
                .ReturnsAsync((Cart?)null);

            // Act
            var result = await _orderService.PlaceOrderAsync(userId, new CheckoutDTO());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task PlaceOrderAsync_ShouldCreateOrder_WhenCartHasItems()
        {
            // Arrange
            string userId = "user-123";
            var cart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2, Product = new Product { Id = 1, Name = "Book 1", Price = 100, Quantity = 10 } }
                }
            };

            _mockCartRepo.Setup(u => u.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);
            _mockOrderRepo.Setup(o => o.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            _mockFlashSales.Setup(f => f.GetActiveSaleByProductIdAsync(It.IsAny<int>())).ReturnsAsync((FlashSale?)null);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var checkoutDto = new CheckoutDTO
            {
                ShippingName = "Recipient",
                ShippingPhone = "0123456789",
                ShippingAddress = "123 Street",
                PaymentMethod = PaymentMethod.COD,
            };

            // Act
            var result = await _orderService.PlaceOrderAsync(userId, checkoutDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(OrderStatus.Pending.ToString(), result.Status);
            _mockOrderRepo.Verify(o => o.AddAsync(It.Is<Order>(o => o.PaymentMethod == checkoutDto.PaymentMethod)), Times.Once);
            _mockCartRepo.Verify(u => u.ClearCartAsync(userId), Times.Once);
        }
    }
}
