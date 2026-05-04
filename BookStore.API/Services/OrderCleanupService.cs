using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BookStore.API.Services
{
    public class OrderCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OrderCleanupService> _logger;

        public OrderCleanupService(IServiceProvider services, ILogger<OrderCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

                        // Lấy đơn hàng Pending > 15 phút
                        var expirationTime = DateTime.Now.AddMinutes(-15);
                        var expiredOrders = await unitOfWork.Orders.GetAllAsync();
                        var ordersToCancel = expiredOrders.Where(o => o.Status == OrderStatus.Pending && o.CreatedAt <= expirationTime).ToList();

                        foreach (var order in ordersToCancel)
                        {
                            _logger.LogInformation($"Cancelling expired order: {order.OrderNumber}");
                            await orderService.CancelExpiredOrderAsync(order.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during order cleanup.");
                }

                // Chờ 5 phút trước khi quét tiếp
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
