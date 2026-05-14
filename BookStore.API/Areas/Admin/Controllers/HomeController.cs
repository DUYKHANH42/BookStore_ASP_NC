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
            var data = await _dashboardService.GetDashboardDataAsync();
            return View(data);
        }

        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData(DateTime? from, DateTime? to)
        {
            var data = await _dashboardService.GetDashboardDataAsync(from, to);
            return Json(data);
        }

        [HttpGet("GetDashboardPartial")]
        public async Task<IActionResult> GetDashboardPartial(DateTime? from, DateTime? to)
        {
            var data = await _dashboardService.GetDashboardDataAsync(from, to);
            return PartialView("_DashboardContent", data);
        }

        [HttpGet("/Admin")]
        public IActionResult Default() => RedirectToAction("Index");
    }
}
