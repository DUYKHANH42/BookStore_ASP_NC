using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class DashboardSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }

        public int TotalOrders { get; set; }
        public decimal OrderGrowth { get; set; }

        public int TotalCustomers { get; set; }
        public decimal CustomerGrowth { get; set; }

        public int TotalProducts { get; set; }

        public decimal AverageOrderValue { get; set; }
        public decimal CancellationRate { get; set; }

        // New Metrics from Image
        public decimal ReturningCustomerRate { get; set; } // % khách quay lại
        public decimal ReturningGrowth { get; set; }

        public decimal AverageRating { get; set; } // Đánh giá trung bình
        public int DeadStockCount { get; set; } // Số lượng sách tồn đọng (>30n)
        public int DeadStockGrowth { get; set; }
    }

    public class RevenueDataDTO
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class OrderStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = string.Empty; // Thêm màu sắc
    }

    public class TopProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty; // Thêm thể loại
        public int SoldCount { get; set; }
        public decimal Revenue { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CategoryRevenueDTO
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class DeadStockProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DaysSinceLastOrder { get; set; }
        public int RemainingQuantity { get; set; }
    }

    public class TopCustomerDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string? AvtUrl { get; set; }
    }

    public class RecentReviewDTO
    {
        public string UserName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardDataDTO
    {
        public DashboardSummaryDTO Summary { get; set; } = new();
        public List<RevenueDataDTO> RevenueChart { get; set; } = new();
        public List<OrderStatusCountDTO> OrderStatusChart { get; set; } = new();
        public List<TopProductDTO> TopSellingProducts { get; set; } = new();
        public List<ProductDTO> LowStockProducts { get; set; } = new();
        public List<OrderDTO> RecentOrders { get; set; } = new();
        public List<TopCustomerDTO> TopCustomers { get; set; } = new();
        public List<RecentReviewDTO> RecentReviews { get; set; } = new();
        public List<CategoryRevenueDTO> CategoryRevenue { get; set; } = new(); // Doanh thu theo thể loại
        public List<DeadStockProductDTO> DeadStockProducts { get; set; } = new(); // Hàng tồn đọng

    }
}
