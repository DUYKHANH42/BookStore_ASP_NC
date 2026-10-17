using BookStore.Application.Services;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class CustomerServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly CustomerService _customerService;

        public CustomerServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockRedisService = new Mock<IRedisService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IRedisService)))
                .Returns(_mockRedisService.Object);

            _customerService = new CustomerService(_mockUserManager.Object, _mockServiceProvider.Object);
        }

        [Fact]
        public async Task ResetCustomerPasswordAsync_ShouldRevokeSessions_WhenPasswordResetSucceeds()
        {
            // Arrange
            string customerId = "user-123";
            string newPassword = "NewPassword123!";
            var user = new ApplicationUser { Id = customerId, TokenVersion = 1 };

            _mockUserManager.Setup(m => m.FindByIdAsync(customerId)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddPasswordAsync(user, newPassword)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _customerService.ResetCustomerPasswordAsync(customerId, newPassword);

            // Assert
            Assert.True(result);
            Assert.Equal(2, user.TokenVersion); // TokenVersion incremented

            _mockRedisService.Verify(r => r.SetAsync($"TokenVersion:{customerId}", 2, null), Times.Once);
            _mockRedisService.Verify(r => r.RemoveAsync($"RefreshToken:{customerId}"), Times.Once);
            _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        }
    }
}
