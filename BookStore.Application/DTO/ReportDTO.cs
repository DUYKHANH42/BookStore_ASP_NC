using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class ReportSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int NewCustomers { get; set; }
        public decimal AverageOrderValue => TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
    }

    public class DailyRevenueDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }


    public class FullReportDTO
    {
        public ReportSummaryDTO Summary { get; set; }
        public List<DailyRevenueDTO> DailyRevenues { get; set; }
        public List<TopProductDTO> TopProducts { get; set; }
        public List<TopCustomerDTO> TopCustomers { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class ReportFilterDTO
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public string? Status { get; set; }
        public string Period { get; set; } = "day";
    }

    public class ReportOptionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EnterpriseReportDTO
    {
        public ReportFilterDTO Filter { get; set; } = new();
        public ReportKpiDTO Kpis { get; set; } = new();
        public List<RevenueTrendDTO> RevenueTrends { get; set; } = new();
        public List<ProductReportRowDTO> ProductRows { get; set; } = new();
        public List<CustomerReportRowDTO> CustomerRows { get; set; } = new();
        public List<NewCustomerTrendDTO> NewCustomerTrends { get; set; } = new();
        public List<CustomerGeoDTO> CustomerGeography { get; set; } = new();
        public List<OrderStatusReportDTO> OrderStatuses { get; set; } = new();
        public List<FlashSaleReportRowDTO> FlashSaleRows { get; set; } = new();
        public List<ReportOptionDTO> Categories { get; set; } = new();
        public List<ReportOptionDTO> Products { get; set; } = new();
    }

    public class ReportKpiDTO
    {
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal PreviousNetRevenue { get; set; }
        public decimal RevenueGrowthRate { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal CancellationRate { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public int TotalUnitsSold { get; set; }
    }

    public class RevenueTrendDTO
    {
        public string Label { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class ProductReportRowDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public int Stock { get; set; }
        public decimal SellThroughRate { get; set; }
        public string VelocityLabel { get; set; } = string.Empty;
    }

    public class CustomerReportRowDTO
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int RecencyDays { get; set; }
        public string Segment { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class NewCustomerTrendDTO
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CustomerGeoDTO
    {
        public string Location { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class OrderStatusReportDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class FlashSaleReportRowDTO
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal SoldStockRatio { get; set; }
        public string PerformanceLabel { get; set; } = string.Empty;
    }
}
