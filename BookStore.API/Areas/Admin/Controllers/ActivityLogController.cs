using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class ActivityLogController : Controller
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            string search = "",
            string module = "",
            string filterAction = "",
            string fromDate = "",
            string toDate = "")
        {
            DateTime? from = null, to = null;
            if (DateTime.TryParse(fromDate, out var f)) from = f;
            if (DateTime.TryParse(toDate, out var t)) to = t;

            var filter = new ActivityLogFilterDTO
            {
                Search = search,
                Module = string.IsNullOrWhiteSpace(module) ? null : module,
                Action = string.IsNullOrWhiteSpace(filterAction) ? null : filterAction,
                FromDate = from,
                ToDate = to
            };

            var result = await _activityLogService.GetPagedAsync(page, 20, filter);
            ViewBag.Search = search;
            ViewBag.Module = module;
            ViewBag.FilterAction = filterAction;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            return View(result);
        }
    }
}
