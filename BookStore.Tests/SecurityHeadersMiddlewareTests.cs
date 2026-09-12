using System.Threading.Tasks;
using BookStore.API.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BookStore.Tests
{
    public class SecurityHeadersMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldAddSecurityHeaders_WhenHeadersAreMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            RequestDelegate next = (ctx) => Task.CompletedTask;
            var middleware = new SecurityHeadersMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
            Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"].ToString());
            Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotOverwriteExistingHeaders()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Headers["X-Frame-Options"] = "DENY";
            RequestDelegate next = (ctx) => Task.CompletedTask;
            var middleware = new SecurityHeadersMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        }
    }
}
