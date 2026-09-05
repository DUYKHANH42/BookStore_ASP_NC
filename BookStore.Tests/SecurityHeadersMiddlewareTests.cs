using BookStore.API.Middleware;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldAddSecurityHeadersToResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            RequestDelegate next = (ctx) => Task.CompletedTask;
            var middleware = new SecurityHeadersMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
            Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        }
    }
}
