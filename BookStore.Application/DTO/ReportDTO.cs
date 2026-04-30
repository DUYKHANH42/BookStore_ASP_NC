using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class ReportSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal NetRevenue { get; set; } // Only Completed
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


    public class TopCustomerDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
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
}
