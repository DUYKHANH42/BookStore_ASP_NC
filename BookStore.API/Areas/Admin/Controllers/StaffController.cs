using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class StaffController : Controller
    {
        private readonly StaffService _staffService;
        private readonly IActivityLogService _activityLog;

        public StaffController(StaffService staffService, IActivityLogService activityLog)
        {
            _staffService = staffService;
            _activityLog = activityLog;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string search = "", string role = "", string isActive = "")
        {
            bool? activeFilter = null;
            if (isActive == "true") activeFilter = true;
            if (isActive == "false") activeFilter = false;

            var result = await _staffService.GetPagedStaffAsync(
                page, 10, search,
                string.IsNullOrWhiteSpace(role) ? null : role,
                activeFilter);

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
                return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            var (success, message, staff) = await _staffService.CreateStaffAsync(adminId, dto);
            return Json(new { success, message, staff });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, bool isActive)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _staffService.UpdateStaffStatusAsync(id, isActive, adminId);
            return Json(new
            {
                success = result,
                message = result
                    ? "Cập nhật trạng thái thành công"
                    : (adminId == id && !isActive
                        ? "Không thể tự khóa tài khoản admin đang đăng nhập"
                        : "Cập nhật thất bại")
            });
        }
    }
}
