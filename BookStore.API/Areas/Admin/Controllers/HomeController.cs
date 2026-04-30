using BookStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            // Mặc định lấy dữ liệu 30 ngày gần nhất
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-30);
            var data = await _dashboardService.GetDashboardDataAsync(startDate, endDate);
            return View(data);
        }

        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData(DateTime? from, DateTime? to)
        {
            var endDate = to ?? DateTime.Now;
            var startDate = from ?? endDate.AddDays(-30);

            var data = await _dashboardService.GetDashboardDataAsync(startDate, endDate);
            return Json(data);
        }

        [HttpGet("/Admin")]
        public IActionResult Default() => RedirectToAction("Index");
    }
}
