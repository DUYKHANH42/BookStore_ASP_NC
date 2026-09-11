using BookStore.API.Middleware;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldAddSecurityHeaders_ToResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            RequestDelegate next = (innerContext) => Task.CompletedTask;
            var middleware = new SecurityHeadersMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
            Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
        }
    }
}
