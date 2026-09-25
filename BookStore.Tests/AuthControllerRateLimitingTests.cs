using System.Linq;
using System.Reflection;
using BookStore.API.Areas.Customer.Controllers;
using BookStore.Application.DTO.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace BookStore.Tests
{
    public class AuthControllerRateLimitingTests
    {
        [Theory]
        [InlineData("Register", typeof(RegisterDto))]
        [InlineData("Login", typeof(LoginDto))]
        public void AuthController_Endpoints_ShouldHaveRateLimitingAttribute(string methodName, System.Type paramType)
        {
            // Arrange
            var method = typeof(AuthController).GetMethod(methodName, new[] { paramType });

            // Assert
            Assert.NotNull(method);

            var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal("auth-limiter", attribute.PolicyName);
        }
    }
}
