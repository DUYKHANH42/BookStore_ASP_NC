using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<DashboardDataDTO> GetDashboardDataAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(startDate, endDate);
            var allProducts = await _unitOfWork.Products.GetAllAsync();
            var totalCustomers = await _userManager.Users.CountAsync();

            var data = new DashboardDataDTO();

            // 1. Tổng quan (Summary)
            data.Summary = new DashboardSummaryDTO
            {
                TotalOrders = orders.Count(),
                TotalRevenue = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalPrice),
                TotalProducts = allProducts.Count(),
                TotalCustomers = totalCustomers
            };

            // 2. Dữ liệu biểu đồ doanh thu (Revenue Chart)
            data.RevenueChart = orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueDataDTO
                {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.Sum(o => o.TotalPrice)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // 3. Phân bổ trạng thái đơn hàng (Order Status Chart)
            data.OrderStatusChart = orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusCountDTO
                {
                    Status = GetStatusVietnamese(g.Key),
                    Count = g.Count()
                })
                .ToList();

            // 4. Top sản phẩm bán chạy
            data.TopSellingProducts = orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.Price)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Join(allProducts, x => x.ProductId, b => b.Id, (x, b) => new TopProductDTO
                {
                    Name = b.Name,
                    SoldCount = x.Count,
                    Revenue = x.Revenue
                })
                .ToList();

            // 5. Cảnh báo tồn kho
            data.LowStockProducts = allProducts
                .Where(b => b.Quantity < 10)
                .Select(b => new ProductDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    Quantity = b.Quantity,
                    Price = b.Price,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            return data;
        }

        private string GetStatusVietnamese(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Paid => "Đã thanh toán",
                OrderStatus.Cancelled => "Đã hủy",
                OrderStatus.Shipping => "Đang giao hàng",
                OrderStatus.Completed => "Hoàn thành",
                _ => status.ToString()
            };
        }
    }
}
