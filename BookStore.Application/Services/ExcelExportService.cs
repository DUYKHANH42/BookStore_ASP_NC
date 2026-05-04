using BookStore.Application.DTO;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BookStore.Application.Services
{
    public class ExcelExportService
    {
        public byte[] ExportOrdersToExcel(IEnumerable<OrderFullDetailDTO> orders)
        {
            using (var workbook = new XLWorkbook())
            {
                // SHEET 1: TỔNG QUAN (DASHBOARD)
                var wsSummary = workbook.Worksheets.Add("Tổng quan");
                SetupSummarySheet(wsSummary, orders);

                // SHEET 2: DANH SÁCH ĐƠN HÀNG
                var wsOrders = workbook.Worksheets.Add("Danh sách đơn hàng");
                SetupOrdersSheet(wsOrders, orders);

                // SHEET 3: CHI TIẾT SẢN PHẨM BÁN RA
                var wsItems = workbook.Worksheets.Add("Phân tích sản phẩm");
                SetupItemsSheet(wsItems, orders);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private void SetupSummarySheet(IXLWorksheet ws, IEnumerable<OrderFullDetailDTO> orders)
        {
            ws.Cell(1, 1).Value = "BÁO CÁO KẾT QUẢ KINH DOANH";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Range(1, 1, 1, 4).Merge();

            ws.Cell(2, 1).Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Thống kê trạng thái
            ws.Cell(4, 1).Value = "Thống kê theo trạng thái";
            ws.Cell(4, 1).Style.Font.Bold = true;

            var stats = orders.GroupBy(o => o.Status)
                              .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalPrice) })
                              .ToList();

            ws.Cell(5, 1).Value = "Trạng thái";
            ws.Cell(5, 2).Value = "Số lượng đơn";
            ws.Cell(5, 3).Value = "Tổng tiền (₫)";
            ws.Range(5, 1, 5, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            ws.Range(5, 1, 5, 3).Style.Font.FontColor = XLColor.White;
            ws.Range(5, 1, 5, 3).Style.Font.Bold = true;

            int row = 6;
            foreach (var s in stats)
            {
                ws.Cell(row, 1).Value = s.Status;
                ws.Cell(row, 2).Value = s.Count;
                ws.Cell(row, 3).Value = s.Total;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                row++;
            }

            // Tổng doanh thu thực tế (Completed)
            var actualRevenue = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalPrice);
            ws.Cell(row + 1, 1).Value = "DOANH THU THỰC TẾ (COMPLETED)";
            ws.Cell(row + 1, 1).Style.Font.Bold = true;
            ws.Cell(row + 1, 3).Value = actualRevenue;
            ws.Cell(row + 1, 3).Style.Font.Bold = true;
            ws.Cell(row + 1, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row + 1, 3).Style.Font.FontColor = XLColor.Green;

            ws.Columns().AdjustToContents();
        }

        private void SetupOrdersSheet(IXLWorksheet ws, IEnumerable<OrderFullDetailDTO> orders)
        {
            var headers = new[] { "Mã đơn hàng", "Khách hàng", "Số điện thoại", "Ngày đặt", "Tổng tiền (₫)", "Trạng thái", "Thanh toán", "Địa chỉ giao hàng" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var o in orders)
            {
                ws.Cell(row, 1).Value = o.OrderNumber;
                ws.Cell(row, 2).Value = o.ShippingName;
                ws.Cell(row, 3).Value = o.ShippingPhone;
                ws.Cell(row, 4).Value = o.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 5).Value = o.TotalPrice;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 6).Value = o.Status;
                ws.Cell(row, 7).Value = o.PaymentMethod;
                ws.Cell(row, 8).Value = o.ShippingAddress;
                row++;
            }

            ws.RangeUsed().SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        private void SetupItemsSheet(IXLWorksheet ws, IEnumerable<OrderFullDetailDTO> orders)
        {
            var headers = new[] { "Mã đơn hàng", "Ngày đặt", "Sản phẩm", "Số lượng", "Đơn giá", "Thành tiền", "Trạng thái đơn" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var o in orders)
            {
                foreach (var item in o.Items)
                {
                    ws.Cell(row, 1).Value = o.OrderNumber;
                    ws.Cell(row, 2).Value = o.CreatedAt.ToString("dd/MM/yyyy");
                    ws.Cell(row, 3).Value = item.ProductName;
                    ws.Cell(row, 4).Value = item.Quantity;
                    ws.Cell(row, 5).Value = item.Price;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 6).Value = item.SubTotal;
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 7).Value = o.Status;
                    row++;
                }
            }

            ws.RangeUsed().SetAutoFilter();
            ws.Columns().AdjustToContents();
        }
    }
}
