using BookStore.Application.DTO;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BookStore.Application.Services
{
    public class ReportExportService
    {
        private static readonly CultureInfo VnCulture = CultureInfo.GetCultureInfo("vi-VN");

        public byte[] ExportExcel(EnterpriseReportDTO report)
        {
            using var workbook = new XLWorkbook();
            SetupOverviewSheet(workbook.Worksheets.Add("Tong quan"), report);
            SetupRevenueSheet(workbook.Worksheets.Add("Doanh thu"), report);
            SetupProductSheet(workbook.Worksheets.Add("San pham"), report);
            SetupCustomerSheet(workbook.Worksheets.Add("Khach hang"), report);
            SetupOrderSheet(workbook.Worksheets.Add("Don hang"), report);
            SetupFlashSaleSheet(workbook.Worksheets.Add("Flash Sale"), report);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportPdf(EnterpriseReportDTO report)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(28);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("BÁO CÁO CHI TIẾT KINH DOANH")
                            .FontSize(18).Bold().FontColor("#0f172a");
                        column.Item().Text($"Kỳ báo cáo: {report.Filter.FromDate:dd/MM/yyyy} - {report.Filter.ToDate:dd/MM/yyyy}")
                            .FontSize(10).FontColor("#64748b");
                    });

                    page.Content().PaddingVertical(16).Column(column =>
                    {
                        column.Spacing(14);
                        column.Item().Element(c => ComposeKpis(c, report));
                        column.Item().Element(c => ComposeRevenueTable(c, report));
                        column.Item().Element(c => ComposeProductTable(c, report));
                        column.Item().Element(c => ComposeCustomerTable(c, report));
                        column.Item().Element(c => ComposeFlashSaleTable(c, report));
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Xuất lúc ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", VnCulture));
                        text.Span(" | Trang ");
                        text.CurrentPageNumber();
                        text.Span("/");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void SetupOverviewSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            ws.Cell(1, 1).Value = "BÁO CÁO CHI TIẾT KINH DOANH";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 18;
            ws.Range(1, 1, 1, 5).Merge();
            ws.Cell(2, 1).Value = $"Kỳ báo cáo: {report.Filter.FromDate:dd/MM/yyyy} - {report.Filter.ToDate:dd/MM/yyyy}";

            var rows = new[]
            {
                ("Doanh thu gộp", report.Kpis.GrossRevenue.ToString("N0", VnCulture)),
                ("Doanh thu thuần", report.Kpis.NetRevenue.ToString("N0", VnCulture)),
                ("Tăng trưởng so với kỳ trước", report.Kpis.RevenueGrowthRate.ToString("N2", VnCulture) + "%"),
                ("Tổng đơn hàng", report.Kpis.TotalOrders.ToString("N0", VnCulture)),
                ("Giá trị đơn trung bình", report.Kpis.AverageOrderValue.ToString("N0", VnCulture)),
                ("Tỷ lệ hủy", report.Kpis.CancellationRate.ToString("N2", VnCulture) + "%"),
                ("Khách hàng mới", report.Kpis.NewCustomers.ToString("N0", VnCulture)),
                ("Khách hàng quay lại", report.Kpis.ReturningCustomers.ToString("N0", VnCulture))
            };

            ws.Cell(4, 1).Value = "Chỉ số";
            ws.Cell(4, 2).Value = "Giá trị";
            StyleHeader(ws.Range(4, 1, 4, 2));
            for (var i = 0; i < rows.Length; i++)
            {
                ws.Cell(i + 5, 1).Value = rows[i].Item1;
                ws.Cell(i + 5, 2).Value = rows[i].Item2;
            }
            ws.Columns().AdjustToContents();
        }

        private static void SetupRevenueSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            WriteTableHeader(ws, new[] { "Kỳ", "Doanh thu", "Số đơn", "AOV" });
            var row = 2;
            foreach (var item in report.RevenueTrends)
            {
                ws.Cell(row, 1).Value = item.Label;
                ws.Cell(row, 2).Value = item.Revenue;
                ws.Cell(row, 3).Value = item.OrderCount;
                ws.Cell(row, 4).Value = item.AverageOrderValue;
                row++;
            }
            FormatMoneyColumns(ws, 2, 4);
        }

        private static void SetupProductSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            WriteTableHeader(ws, new[] { "Sản phẩm", "Danh mục", "Đã bán", "Doanh thu", "Tồn kho", "Sell-through", "Nhận định" });
            var row = 2;
            foreach (var item in report.ProductRows)
            {
                ws.Cell(row, 1).Value = item.ProductName;
                ws.Cell(row, 2).Value = item.CategoryName;
                ws.Cell(row, 3).Value = item.UnitsSold;
                ws.Cell(row, 4).Value = item.Revenue;
                ws.Cell(row, 5).Value = item.Stock;
                ws.Cell(row, 6).Value = item.SellThroughRate / 100;
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 7).Value = item.VelocityLabel;
                row++;
            }
            FormatMoneyColumns(ws, 4, 4);
        }

        private static void SetupCustomerSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            WriteTableHeader(ws, new[] { "Khách hàng", "Email", "Số đơn", "Tổng chi", "Lần mua gần nhất", "RFM", "Khu vực" });
            var row = 2;
            foreach (var item in report.CustomerRows)
            {
                ws.Cell(row, 1).Value = item.CustomerName;
                ws.Cell(row, 2).Value = item.Email;
                ws.Cell(row, 3).Value = item.OrderCount;
                ws.Cell(row, 4).Value = item.TotalSpent;
                ws.Cell(row, 5).Value = item.LastOrderAt?.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 6).Value = item.Segment;
                ws.Cell(row, 7).Value = item.Location;
                row++;
            }
            FormatMoneyColumns(ws, 4, 4);
        }

        private static void SetupOrderSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            WriteTableHeader(ws, new[] { "Trạng thái", "Số đơn", "Doanh thu", "Tỷ trọng" });
            var row = 2;
            foreach (var item in report.OrderStatuses)
            {
                ws.Cell(row, 1).Value = item.Status;
                ws.Cell(row, 2).Value = item.Count;
                ws.Cell(row, 3).Value = item.Revenue;
                ws.Cell(row, 4).Value = item.Percentage / 100;
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
                row++;
            }
            FormatMoneyColumns(ws, 3, 3);
        }

        private static void SetupFlashSaleSheet(IXLWorksheet ws, EnterpriseReportDTO report)
        {
            WriteTableHeader(ws, new[] { "Chiến dịch", "Bắt đầu", "Kết thúc", "Stock", "Đã bán", "Doanh thu", "Tỷ lệ bán", "Nhận định" });
            var row = 2;
            foreach (var item in report.FlashSaleRows)
            {
                ws.Cell(row, 1).Value = item.CampaignName;
                ws.Cell(row, 2).Value = item.StartTime.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 3).Value = item.EndTime.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = item.SaleStock;
                ws.Cell(row, 5).Value = item.SoldCount;
                ws.Cell(row, 6).Value = item.Revenue;
                ws.Cell(row, 7).Value = item.SoldStockRatio / 100;
                ws.Cell(row, 7).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 8).Value = item.PerformanceLabel;
                row++;
            }
            FormatMoneyColumns(ws, 6, 6);
        }

        private static void ComposeKpis(IContainer container, EnterpriseReportDTO report)
        {
            container.Row(row =>
            {
                row.RelativeItem().Element(c => KpiCard(c, "Doanh thu thuần", report.Kpis.NetRevenue.ToString("N0", VnCulture)));
                row.RelativeItem().Element(c => KpiCard(c, "Tăng trưởng", report.Kpis.RevenueGrowthRate.ToString("N2", VnCulture) + "%"));
                row.RelativeItem().Element(c => KpiCard(c, "Tổng đơn", report.Kpis.TotalOrders.ToString("N0", VnCulture)));
                row.RelativeItem().Element(c => KpiCard(c, "Tỷ lệ hủy", report.Kpis.CancellationRate.ToString("N2", VnCulture) + "%"));
            });
        }

        private static void KpiCard(IContainer container, string label, string value)
        {
            container.PaddingRight(6).Border(1).BorderColor("#e2e8f0").Padding(10).Column(column =>
            {
                column.Item().Text(label).FontSize(8).FontColor("#64748b");
                column.Item().Text(value).FontSize(13).Bold().FontColor("#0f172a");
            });
        }

        private static void ComposeRevenueTable(IContainer container, EnterpriseReportDTO report)
        {
            ComposeSimpleTable(container, "Doanh thu theo kỳ", new[] { "Kỳ", "Doanh thu", "Đơn" },
                report.RevenueTrends.Take(12).Select(x => new[] { x.Label, x.Revenue.ToString("N0", VnCulture), x.OrderCount.ToString("N0", VnCulture) }).ToArray());
        }

        private static void ComposeProductTable(IContainer container, EnterpriseReportDTO report)
        {
            ComposeSimpleTable(container, "Top sản phẩm", new[] { "Sản phẩm", "Đã bán", "Doanh thu", "Tồn" },
                report.ProductRows.Take(10).Select(x => new[] { x.ProductName, x.UnitsSold.ToString("N0", VnCulture), x.Revenue.ToString("N0", VnCulture), x.Stock.ToString("N0", VnCulture) }).ToArray());
        }

        private static void ComposeCustomerTable(IContainer container, EnterpriseReportDTO report)
        {
            ComposeSimpleTable(container, "Khách hàng giá trị", new[] { "Khách hàng", "Số đơn", "Tổng chi", "RFM" },
                report.CustomerRows.Take(10).Select(x => new[] { x.CustomerName, x.OrderCount.ToString("N0", VnCulture), x.TotalSpent.ToString("N0", VnCulture), x.Segment }).ToArray());
        }

        private static void ComposeFlashSaleTable(IContainer container, EnterpriseReportDTO report)
        {
            ComposeSimpleTable(container, "Hiệu quả Flash Sale", new[] { "Chiến dịch", "Đã bán", "Stock", "Tỷ lệ" },
                report.FlashSaleRows.Take(10).Select(x => new[] { x.CampaignName, x.SoldCount.ToString("N0", VnCulture), x.SaleStock.ToString("N0", VnCulture), x.SoldStockRatio.ToString("N2", VnCulture) + "%" }).ToArray());
        }

        private static void ComposeSimpleTable(IContainer container, string title, string[] headers, string[][] rows)
        {
            container.Column(column =>
            {
                column.Item().Text(title).FontSize(12).Bold().FontColor("#0f172a");
                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var item in headers)
                            header.Cell().Background("#0f172a").Padding(5).Text(item).FontColor(Colors.White).Bold().FontSize(8);
                    });

                    foreach (var row in rows)
                        foreach (var cell in row)
                            table.Cell().BorderBottom(1).BorderColor("#e2e8f0").Padding(5).Text(cell).FontSize(8);
                });
            });
        }

        private static void WriteTableHeader(IXLWorksheet ws, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            StyleHeader(ws.Range(1, 1, 1, headers.Length));
            ws.SheetView.FreezeRows(1);
        }

        private static void StyleHeader(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
        }

        private static void FormatMoneyColumns(IXLWorksheet ws, int fromColumn, int toColumn)
        {
            for (var column = fromColumn; column <= toColumn; column++)
                ws.Column(column).Style.NumberFormat.Format = "#,##0";
            ws.RangeUsed()?.SetAutoFilter();
            ws.Columns().AdjustToContents();
        }
    }
}
