using BookStore.Application.DTO.Auth;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IAuthService> _mockIdentityService;
        private readonly Mock<IMailService> _mockMailService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockIdentityService = new Mock<IAuthService>();
            _mockMailService = new Mock<IMailService>();
            _mockFileService = new Mock<IFileService>();
            _mockConfig = new Mock<IConfiguration>();

            _authService = new AuthService(
                _mockIdentityService.Object, 
                _mockMailService.Object, 
                _mockFileService.Object, 
                _mockConfig.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var registerDto = new RegisterDto 
            { 
                Email = "test@example.com", 
                FullName = "Test User", 
                Password = "Password123!" 
            };

            _mockIdentityService.Setup(s => s.RegisterAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.True(result.IsSuccess);
            _mockIdentityService.Verify(s => s.RegisterAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), "Customer"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var expectedModel = new AuthModel 
            { 
                Token = "fake-jwt-token", 
                User = new ApplicationUser { Email = "test@example.com" },
                Roles = new List<string> { "Customer" }
            };

            _mockIdentityService.Setup(s => s.LoginAsync(loginDto.Email, loginDto.Password))
                .ReturnsAsync(expectedModel);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("fake-jwt-token", result.Token);
        }
    }
}
