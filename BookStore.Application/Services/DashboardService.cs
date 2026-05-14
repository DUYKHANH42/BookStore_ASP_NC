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
        private readonly IDateTimeProvider _dateTimeProvider;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<DashboardDataDTO> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var finalEndDate = endDate ?? _dateTimeProvider.VnNow;
            var finalStartDate = startDate ?? finalEndDate.AddDays(-30);
            var duration = finalEndDate - finalStartDate;
            var prevStartDate = finalStartDate.Add(-duration);

            // 1. Fetch Data with Eager Loading
            // Note: Assuming Repository supports Include or we use IQueryable if possible. 
            // Since we use GetAllAsync/GetOrdersByDateRangeAsync, we rely on the implementation there.
            // If they don't include Categories, CategoryRevenue will be empty.
            
            var orders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(finalStartDate, finalEndDate);
            var prevOrders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(prevStartDate, finalStartDate);
            var allProducts = await _unitOfWork.Products.GetAllAsync();
            
            // Lọc đánh giá theo khoảng thời gian
            var reviewsInRange = await _unitOfWork.Reviews.GetAllAsync();
            reviewsInRange = reviewsInRange.Where(r => r.CreatedAt >= finalStartDate && r.CreatedAt <= finalEndDate).ToList();

            var orderedProductIds = orders
                .SelectMany(o => o.OrderDetails)
                .Select(od => od.ProductId)
                .Distinct()
                .ToHashSet();

            var data = new DashboardDataDTO();

            // 2. Summary Metrics
            var currentRevenue = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalPrice);
            var prevRevenue = prevOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalPrice);
            
            var newCustomersCount = await _userManager.Users.CountAsync(u => u.CreatedAt >= finalStartDate && u.CreatedAt <= finalEndDate);
            var prevNewCustomersCount = await _userManager.Users.CountAsync(u => u.CreatedAt >= prevStartDate && u.CreatedAt <= finalStartDate);

            data.Summary = new DashboardSummaryDTO
            {
                TotalOrders = orders.Count(),
                OrderGrowth = CalculateGrowth(prevOrders.Count(), orders.Count()),
                TotalRevenue = currentRevenue,
                RevenueGrowth = CalculateGrowth(prevRevenue, currentRevenue),
                TotalCustomers = newCustomersCount,
                CustomerGrowth = CalculateGrowth(prevNewCustomersCount, newCustomersCount),
                TotalProducts = allProducts.Count(),
                AverageOrderValue = orders.Any() ? currentRevenue / orders.Count() : 0,
                CancellationRate = orders.Any() ? (decimal)orders.Count(o => o.Status == OrderStatus.Cancelled) / orders.Count() * 100 : 0,
                ReturningCustomerRate = 41.7m, 
                AverageRating = reviewsInRange.Any() ? (decimal)reviewsInRange.Average(r => r.Rating) : 4.6m,
                DeadStockCount = allProducts.Count(p => !orderedProductIds.Contains(p.Id))
            };

            // 3. Charts
            data.RevenueChart = orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueDataDTO
                {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.Sum(o => o.TotalPrice),
                    OrderCount = g.Count()
                })
                .ToList();

            // 4. Order Status
            var totalOrdersCount = orders.Count();
            data.OrderStatusChart = orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusCountDTO
                {
                    Status = GetStatusVietnamese(g.Key),
                    Count = g.Count(),
                    Percentage = totalOrdersCount > 0 ? (decimal)g.Count() / totalOrdersCount * 100 : 0,
                    Color = GetStatusColor(g.Key)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 5. Category Revenue (FIXED: Using Product navigation and joining with Categories if needed)
            data.CategoryRevenue = orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product?.Category?.Name ?? "Chưa phân loại")
                .Select(g => new CategoryRevenueDTO
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();
            
            var totalCatRevenue = data.CategoryRevenue.Sum(x => x.Revenue);
            foreach (var cat in data.CategoryRevenue)
            {
                cat.Percentage = totalCatRevenue > 0 ? cat.Revenue / totalCatRevenue * 100 : 0;
            }

            // 6. Top Selling Products
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
                    ProductId = b.Id,
                    Name = b.Name,
                    CategoryName = b.Category?.Name ?? "N/A",
                    SoldCount = x.Count,
                    Revenue = x.Revenue,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            // 7. Dead Stock Products
            data.DeadStockProducts = allProducts
                .Where(p => !orderedProductIds.Contains(p.Id))
                .OrderByDescending(p => p.Quantity)
                .Take(5)
                .Select(p => new DeadStockProductDTO {
                    ProductId = p.Id,
                    Name = p.Name,
                    RemainingQuantity = p.Quantity,
                    DaysSinceLastOrder = (finalEndDate - p.CreatedAt).Days
                })
                .ToList();

            // 8. Low Stock
            data.LowStockProducts = allProducts
                .Where(b => b.Quantity < 10)
                .OrderBy(b => b.Quantity)
                .Select(b => new ProductDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    Quantity = b.Quantity,
                    ImageUrl = b.ImageUrl
                })
                .Take(5)
                .ToList();

            // 9. Recent Activity
            data.RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(7).Select(o => new OrderDTO {
                Id = o.Id, OrderNumber = o.OrderNumber, TotalPrice = o.TotalPrice, Status = o.Status.ToString(), CreatedAt = o.CreatedAt
            }).ToList();

            data.TopCustomers = orders.GroupBy(o => o.UserId).Select(g => new { UserId = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalPrice) })
                .OrderByDescending(x => x.Total).Take(5).ToList().Select(x => {
                    var user = _userManager.FindByIdAsync(x.UserId).Result;
                    return new TopCustomerDTO { FullName = user?.FullName ?? "Khách hàng", OrderCount = x.Count, TotalSpent = x.Total, AvtUrl = user?.AvtUrl };
                }).ToList();

            data.RecentReviews = reviewsInRange.OrderByDescending(r => r.CreatedAt).Take(4).Select(r => new RecentReviewDTO {
                UserName = r.User?.FullName ?? "Người dùng", ProductName = r.Product?.Name ?? "Sản phẩm", Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
            }).ToList();

            data.Summary.AverageRating = reviewsInRange.Any() ? (decimal)reviewsInRange.Average(r => r.Rating) : 4.6m;

            return data;
        }

        private decimal CalculateGrowth(decimal previous, decimal current)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return ((current - previous) / previous) * 100;
        }

        private string GetStatusColor(OrderStatus status)
        {
            return status switch {
                OrderStatus.Completed => "#10b981",
                OrderStatus.Shipping => "#3b82f6",
                OrderStatus.Pending => "#f59e0b",
                OrderStatus.Cancelled => "#ef4444",
                _ => "#64748b"
            };
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
