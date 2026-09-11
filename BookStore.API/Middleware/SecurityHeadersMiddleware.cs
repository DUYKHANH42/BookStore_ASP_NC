using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookStore.API.Middleware
{
    /// <summary>
    /// Middleware to append essential HTTP security headers to all HTTP responses.
    /// Protects against Clickjacking, MIME-sniffing, XSS, and info leakage via Referrer.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Set HTTP security headers before passing context down the pipeline
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            await _next(context);
        }
    }
}
