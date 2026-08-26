using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class ReportController : Controller
    {
        private readonly IEnterpriseReportService _reportService;
        private readonly ReportExportService _reportExportService;

        public ReportController(IEnterpriseReportService reportService, ReportExportService reportExportService)
        {
            _reportService = reportService;
            _reportExportService = reportExportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ReportFilterDTO filter)
        {
            var report = await _reportService.GetReportAsync(filter);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] ReportFilterDTO filter)
        {
            var report = await _reportService.GetReportAsync(filter);
            return Json(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterDTO filter)
        {
            var report = await _reportService.GetReportAsync(filter);
            var bytes = _reportExportService.ExportExcel(report);
            var fileName = $"Enterprise_Report_{report.Filter.FromDate:yyyyMMdd}_{report.Filter.ToDate:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterDTO filter)
        {
            var report = await _reportService.GetReportAsync(filter);
            var bytes = _reportExportService.ExportPdf(report);
            var fileName = $"Enterprise_Report_{report.Filter.FromDate:yyyyMMdd}_{report.Filter.ToDate:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
    }
}
