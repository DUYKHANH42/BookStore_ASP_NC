using BookStore.Application.Services;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly InvoiceService _invoiceService;
        private readonly ExcelExportService _excelExportService;
        private readonly IWebHostEnvironment _env;

        public OrderController(OrderService orderService, InvoiceService invoiceService, ExcelExportService excelExportService, IWebHostEnvironment env)
        {
            _orderService = orderService;
            _invoiceService = invoiceService;
            _excelExportService = excelExportService;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1, string status = "", string search = "")
        {
            int pageSize = 10;
            var result = await _orderService.GetPagedOrdersAsync(page, pageSize, status, search);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null) return NotFound();
            return PartialView("_OrderDetailPartial", order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var adminName = User.Identity?.Name ?? "Admin";
            var result = await _orderService.UpdateOrderStatusAsync(id, status, adminName);
            if (result)
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            
            return Json(new { success = false, message = "Cập nhật thất bại" });
        }
        [HttpGet]
        public async Task<IActionResult> ExportInvoice(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null) return NotFound();

            string logoPath = Path.Combine(_env.WebRootPath, "uploads", "imgs", "logo_sach.png");
            var pdfBytes = _invoiceService.GenerateInvoicePdf(order, logoPath);

            return File(pdfBytes, "application/pdf", $"Invoice_{order.OrderNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string status = "", string search = "")
        {
            var orders = await _orderService.GetAllOrdersForReportAsync(status, search);
            var excelBytes = _excelExportService.ExportOrdersToExcel(orders);

            string fileName = $"Orders_Report_{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
