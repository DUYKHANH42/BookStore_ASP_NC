#nullable enable
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BookStore.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BookStore.Domain.Common.NotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                await WriteResponse(context, 404, ex.Message);
            }
            catch (BookStore.Domain.Common.InsufficientStockException ex)
            {
                _logger.LogWarning(ex.Message);
                await WriteResponse(context, 409, ex.Message);
            }
            catch (BookStore.Domain.Common.ConcurrencyException ex)
            {
                _logger.LogWarning(ex.Message);
                await WriteResponse(context, 409, ex.Message);
            }
            catch (BookStore.Domain.Common.BusinessException ex)
            {
                _logger.LogWarning(ex.Message);
                await WriteResponse(context, 400, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var message = _env.IsDevelopment() ? ex.Message : "Internal Server Error";
                var details = _env.IsDevelopment() ? ex.StackTrace : null;
                await WriteResponse(context, 500, message, details);
            }
        }

        private static async Task WriteResponse(HttpContext context, int statusCode, string message, string? details = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            var response = new ApiException(statusCode, message, details);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }

    public class ApiException
    {
        public ApiException(int statusCode, string message, string? details = null)
        {
            StatusCode = statusCode;
            Message = message;
            Details = details;
        }

        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }
    }
}
