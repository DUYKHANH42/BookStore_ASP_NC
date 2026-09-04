#nullable enable
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BookStore.Domain.Common;

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
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.NotFound, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.Forbidden, ex.Message);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.Conflict, ex.Message);
            }
            catch (InsufficientStockException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.BadRequest, ex.Message);
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.Conflict, ex.Message);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                await WriteResponse(context, (int)HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
                var details = _env.IsDevelopment() ? ex.StackTrace : null;
                await WriteResponse(context, (int)HttpStatusCode.InternalServerError, message, details);
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
