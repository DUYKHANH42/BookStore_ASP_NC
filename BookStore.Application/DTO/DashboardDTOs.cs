using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class DashboardSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; } // Đổi từ TotalBooks sang TotalProducts
    }

    public class RevenueDataDTO
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class OrderStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopProductDTO // Đổi từ TopBookDTO sang TopProductDTO
    {
        public string Name { get; set; } = string.Empty; // Title -> Name
        public int SoldCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardDataDTO
    {
        public DashboardSummaryDTO Summary { get; set; } = new();
        public List<RevenueDataDTO> RevenueChart { get; set; } = new();
        public List<OrderStatusCountDTO> OrderStatusChart { get; set; } = new();
        public List<TopProductDTO> TopSellingProducts { get; set; } = new(); // TopSellingBooks -> TopSellingProducts
        public List<ProductDTO> LowStockProducts { get; set; } = new(); // LowStockBooks -> LowStockProducts
    }
}
