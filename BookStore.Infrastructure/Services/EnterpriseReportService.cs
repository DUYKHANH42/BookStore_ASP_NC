using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Services
{
    public class EnterpriseReportService : IEnterpriseReportService
    {
        private readonly BookStoreDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnterpriseReportService(BookStoreDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<EnterpriseReportDTO> GetReportAsync(ReportFilterDTO filter)
        {
            var normalized = NormalizeFilter(filter);
            var from = normalized.FromDate!.Value.Date;
            var to = normalized.ToDate!.Value.Date.AddDays(1).AddTicks(-1);
            var durationDays = Math.Max(1, (to.Date - from.Date).Days + 1);
            var previousFrom = from.AddDays(-durationDays);
            var previousTo = from.AddTicks(-1);

            var orders = await BuildOrderQuery(from, to, normalized).ToListAsync();
            var previousOrders = await BuildOrderQuery(previousFrom, previousTo, normalized).ToListAsync();
            var products = await BuildProductQuery(normalized).ToListAsync();
            var users = await _userManager.Users.AsNoTracking()
                .Select(u => new UserReportData
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Address = u.Address,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var validOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var validItems = validOrders.SelectMany(o => o.Items).ToList();
            var previousNetRevenue = previousOrders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .Sum(o => o.TotalPrice);

            var report = new EnterpriseReportDTO
            {
                Filter = normalized,
                Categories = await _context.Categories.AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => new ReportOptionDTO { Id = c.Id, Name = c.Name })
                    .ToListAsync(),
                Products = await _context.Products.AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Select(p => new ReportOptionDTO { Id = p.Id, Name = p.Name })
                    .ToListAsync()
            };

            report.Kpis = BuildKpis(orders, validOrders, validItems, users, from, to, previousNetRevenue);
            report.RevenueTrends = BuildRevenueTrends(validOrders, normalized.Period);
            report.ProductRows = BuildProductRows(products, validItems);
            report.CustomerRows = BuildCustomerRows(validOrders, users, to);
            report.NewCustomerTrends = BuildNewCustomerTrends(users, from, to);
            report.CustomerGeography = BuildCustomerGeography(users, orders);
            report.OrderStatuses = BuildOrderStatuses(orders);
            report.FlashSaleRows = await BuildFlashSaleRows(from, to, normalized, validItems);

            return report;
        }

        private IQueryable<OrderReportData> BuildOrderQuery(DateTime from, DateTime to, ReportFilterDTO filter)
        {
            var query = _context.Orders.AsNoTracking()
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to);

            if (TryParseStatus(filter.Status, out var status))
                query = query.Where(o => o.Status == status);

            if (filter.ProductId.HasValue)
                query = query.Where(o => o.OrderDetails.Any(od => od.ProductId == filter.ProductId.Value));

            if (filter.CategoryId.HasValue)
                query = query.Where(o => o.OrderDetails.Any(od => od.Product.CategoryId == filter.CategoryId.Value));

            return query.Select(o => new OrderReportData
            {
                Id = o.Id,
                UserId = o.UserId ?? "",
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalPrice = o.TotalPrice,
                Items = o.OrderDetails
                    .Where(od => !filter.ProductId.HasValue || od.ProductId == filter.ProductId.Value)
                    .Where(od => !filter.CategoryId.HasValue || od.Product.CategoryId == filter.CategoryId.Value)
                    .Select(od => new OrderItemReportData
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product.Name,
                        CategoryName = od.Product.Category.Name,
                        Quantity = od.Quantity,
                        Price = od.Price,
                        FlashSaleId = od.FlashSaleId
                    })
                    .ToList()
            });
        }

        private IQueryable<ProductReportData> BuildProductQuery(ReportFilterDTO filter)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (filter.ProductId.HasValue)
                query = query.Where(p => p.Id == filter.ProductId.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            return query.Select(p => new ProductReportData
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category.Name,
                Stock = p.Quantity
            });
        }

        private static ReportFilterDTO NormalizeFilter(ReportFilterDTO filter)
        {
            var to = (filter.ToDate ?? DateTime.Now).Date;
            var from = (filter.FromDate ?? to.AddDays(-30)).Date;
            if (from > to)
                (from, to) = (to, from);

            var period = (filter.Period ?? "day").Trim().ToLowerInvariant();
            if (period != "day" && period != "week" && period != "month" && period != "quarter")
                period = "day";

            return new ReportFilterDTO
            {
                FromDate = from,
                ToDate = to,
                CategoryId = filter.CategoryId,
                ProductId = filter.ProductId,
                Status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim(),
                Period = period
            };
        }

        private static ReportKpiDTO BuildKpis(
            List<OrderReportData> orders,
            List<OrderReportData> validOrders,
            List<OrderItemReportData> validItems,
            List<UserReportData> users,
            DateTime from,
            DateTime to,
            decimal previousNetRevenue)
        {
            var netRevenue = validOrders.Sum(o => o.TotalPrice);
            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            var returningCustomers = validOrders
                .Where(o => !string.IsNullOrWhiteSpace(o.UserId))
                .GroupBy(o => o.UserId)
                .Count(g => g.Count() >= 2);

            return new ReportKpiDTO
            {
                GrossRevenue = orders.Sum(o => o.TotalPrice),
                NetRevenue = netRevenue,
                PreviousNetRevenue = previousNetRevenue,
                RevenueGrowthRate = CalculateGrowth(previousNetRevenue, netRevenue),
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                CancellationRate = orders.Count > 0 ? (decimal)cancelledOrders / orders.Count * 100 : 0,
                AverageOrderValue = validOrders.Count > 0 ? netRevenue / validOrders.Count : 0,
                NewCustomers = users.Count(u => u.CreatedAt >= from && u.CreatedAt <= to),
                ReturningCustomers = returningCustomers,
                TotalUnitsSold = validItems.Sum(i => i.Quantity)
            };
        }

        private static List<RevenueTrendDTO> BuildRevenueTrends(List<OrderReportData> orders, string period)
        {
            return orders
                .GroupBy(o => GetPeriodStart(o.CreatedAt, period))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var revenue = g.Sum(o => o.TotalPrice);
                    var orderCount = g.Count();
                    return new RevenueTrendDTO
                    {
                        PeriodStart = g.Key,
                        Label = GetPeriodLabel(g.Key, period),
                        Revenue = revenue,
                        OrderCount = orderCount,
                        AverageOrderValue = orderCount > 0 ? revenue / orderCount : 0
                    };
                })
                .ToList();
        }

        private static List<ProductReportRowDTO> BuildProductRows(List<ProductReportData> products, List<OrderItemReportData> items)
        {
            var sales = items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => new
                {
                    Units = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.Price)
                });

            return products
                .Select(p =>
                {
                    sales.TryGetValue(p.ProductId, out var sale);
                    var unitsSold = sale?.Units ?? 0;
                    var revenue = sale?.Revenue ?? 0;
                    var sellThrough = unitsSold + p.Stock > 0 ? (decimal)unitsSold / (unitsSold + p.Stock) * 100 : 0;
                    return new ProductReportRowDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CategoryName = p.CategoryName,
                        UnitsSold = unitsSold,
                        Revenue = revenue,
                        Stock = p.Stock,
                        SellThroughRate = sellThrough,
                        VelocityLabel = GetVelocityLabel(unitsSold, p.Stock, sellThrough)
                    };
                })
                .OrderByDescending(x => x.UnitsSold)
                .ThenByDescending(x => x.Revenue)
                .ThenByDescending(x => x.Stock)
                .Take(50)
                .ToList();
        }

        private static List<CustomerReportRowDTO> BuildCustomerRows(List<OrderReportData> orders, List<UserReportData> users, DateTime to)
        {
            var userMap = users.ToDictionary(u => u.Id);

            return orders
                .Where(o => !string.IsNullOrWhiteSpace(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g =>
                {
                    userMap.TryGetValue(g.Key, out var user);
                    var lastOrderAt = g.Max(o => o.CreatedAt);
                    var orderCount = g.Count();
                    var totalSpent = g.Sum(o => o.TotalPrice);
                    return new CustomerReportRowDTO
                    {
                        CustomerId = g.Key,
                        CustomerName = string.IsNullOrWhiteSpace(user?.FullName) ? "Khach hang" : user.FullName,
                        Email = user?.Email ?? "",
                        OrderCount = orderCount,
                        TotalSpent = totalSpent,
                        LastOrderAt = lastOrderAt,
                        RecencyDays = Math.Max(0, (to.Date - lastOrderAt.Date).Days),
                        Segment = GetCustomerSegment(orderCount, totalSpent, Math.Max(0, (to.Date - lastOrderAt.Date).Days)),
                        Location = NormalizeLocation(user?.Address)
                    };
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(50)
                .ToList();
        }

        private static List<NewCustomerTrendDTO> BuildNewCustomerTrends(List<UserReportData> users, DateTime from, DateTime to)
        {
            return users
                .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
                .GroupBy(u => new DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NewCustomerTrendDTO
                {
                    Label = g.Key.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                    Count = g.Count()
                })
                .ToList();
        }

        private static List<CustomerGeoDTO> BuildCustomerGeography(List<UserReportData> users, List<OrderReportData> orders)
        {
            var userLocations = users
                .Where(u => !string.IsNullOrWhiteSpace(u.Address))
                .Select(u => NormalizeLocation(u.Address));

            var orderLocations = orders
                .Where(o => !string.IsNullOrWhiteSpace(o.ShippingAddress))
                .Select(o => NormalizeLocation(o.ShippingAddress));

            return userLocations.Concat(orderLocations)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x)
                .Select(g => new CustomerGeoDTO { Location = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToList();
        }

        private static List<OrderStatusReportDTO> BuildOrderStatuses(List<OrderReportData> orders)
        {
            var total = orders.Count;
            return orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusReportDTO
                {
                    Status = GetStatusVietnamese(g.Key),
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.TotalPrice),
                    Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        private async Task<List<FlashSaleReportRowDTO>> BuildFlashSaleRows(
            DateTime from,
            DateTime to,
            ReportFilterDTO filter,
            List<OrderItemReportData> validItems)
        {
            var revenueByFlashSale = validItems
                .Where(i => i.FlashSaleId.HasValue)
                .GroupBy(i => i.FlashSaleId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Price * i.Quantity));

            var query = _context.FlashSaleCampaigns.AsNoTracking()
                .Where(c => c.StartTime <= to && c.EndTime >= from);

            if (filter.ProductId.HasValue)
                query = query.Where(c => c.FlashSales.Any(fs => fs.ProductId == filter.ProductId.Value));

            if (filter.CategoryId.HasValue)
                query = query.Where(c => c.FlashSales.Any(fs => fs.Product.CategoryId == filter.CategoryId.Value));

            var campaigns = await query
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.StartTime,
                    c.EndTime,
                    Items = c.FlashSales
                        .Where(fs => !filter.ProductId.HasValue || fs.ProductId == filter.ProductId.Value)
                        .Where(fs => !filter.CategoryId.HasValue || fs.Product.CategoryId == filter.CategoryId.Value)
                        .Select(fs => new
                        {
                            fs.Id,
                            fs.SaleStock,
                            fs.SoldCount
                        })
                        .ToList()
                })
                .ToListAsync();

            return campaigns
                .Select(c =>
                {
                    var saleStock = c.Items.Sum(i => i.SaleStock);
                    var soldCount = c.Items.Sum(i => i.SoldCount);
                    var ratio = saleStock > 0 ? (decimal)soldCount / saleStock * 100 : 0;
                    var revenue = c.Items.Sum(i => revenueByFlashSale.TryGetValue(i.Id, out var value) ? value : 0);
                    return new FlashSaleReportRowDTO
                    {
                        CampaignId = c.Id,
                        CampaignName = c.Name,
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        SaleStock = saleStock,
                        SoldCount = soldCount,
                        Revenue = revenue,
                        SoldStockRatio = ratio,
                        PerformanceLabel = ratio >= 80 ? "Rất tốt" : ratio >= 50 ? "Tốt" : ratio > 0 ? "Cần tối ưu" : "Chưa phát sinh"
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ThenByDescending(x => x.SoldStockRatio)
                .Take(30)
                .ToList();
        }

        private static bool TryParseStatus(string? status, out OrderStatus orderStatus)
        {
            orderStatus = default;
            return !string.IsNullOrWhiteSpace(status)
                && Enum.TryParse(status, true, out orderStatus);
        }

        private static decimal CalculateGrowth(decimal previous, decimal current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;
            return (current - previous) / previous * 100;
        }

        private static DateTime GetPeriodStart(DateTime date, string period)
        {
            return period switch
            {
                "week" => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
                "month" => new DateTime(date.Year, date.Month, 1),
                "quarter" => new DateTime(date.Year, ((date.Month - 1) / 3) * 3 + 1, 1),
                _ => date.Date
            };
        }

        private static string GetPeriodLabel(DateTime date, string period)
        {
            return period switch
            {
                "week" => $"Tuan {ISOWeek.GetWeekOfYear(date)}/{date.Year}",
                "month" => date.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                "quarter" => $"Q{((date.Month - 1) / 3) + 1}/{date.Year}",
                _ => date.ToString("dd/MM", CultureInfo.InvariantCulture)
            };
        }

        private static string GetVelocityLabel(int unitsSold, int stock, decimal sellThrough)
        {
            if (unitsSold == 0 && stock > 0)
                return "Bán chậm";
            if (sellThrough >= 70)
                return "Bán chạy";
            if (stock <= 5 && unitsSold > 0)
                return "Sắp hết hàng";
            return "Ổn định";
        }

        private static string GetCustomerSegment(int orderCount, decimal totalSpent, int recencyDays)
        {
            if (orderCount >= 5 && totalSpent >= 3000000 && recencyDays <= 60)
                return "Thân Thiết";
            if (orderCount >= 3 && recencyDays <= 90)
                return "Tiềm năng";
            if (recencyDays > 120)
                return "Cần kích hoạt";
            return "Mới";
        }

        private static string NormalizeLocation(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "Chưa rõ";

            var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? parts[^1] : address.Trim();
        }

        private static string GetStatusVietnamese(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Paid => "Đã thanh toán",
                OrderStatus.Cancelled => "Đã hủy",
                OrderStatus.Shipping => "Đang giao",
                OrderStatus.Completed => "Hoàn thành",
                _ => status.ToString()
            };
        }

        private class OrderReportData
        {
            public int Id { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public OrderStatus Status { get; set; }
            public decimal TotalPrice { get; set; }
            public List<OrderItemReportData> Items { get; set; } = new();
        }

        private class OrderItemReportData
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public int? FlashSaleId { get; set; }
        }

        private class ProductReportData
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Stock { get; set; }
        }

        private class UserReportData
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
