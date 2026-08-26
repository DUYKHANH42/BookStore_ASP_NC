using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;
        private readonly IActivityLogService _activityLog;

        public CustomerController(CustomerService customerService, IActivityLogService activityLog)
        {
            _customerService = customerService;
            _activityLog = activityLog;
        }

        public async Task<IActionResult> Index(int page = 1, string search = "", bool? isActive = null)
        {
            const int pageSize = 10;
            var result = await _customerService.GetPagedCustomersAsync(page, pageSize, search, isActive);
            ViewBag.Search = search;
            ViewBag.IsActive = isActive.HasValue ? isActive.Value.ToString().ToLower() : "";
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(string id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return PartialView("_CustomerDetailPartial", customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, bool isActive)
        {
            var result = await _customerService.UpdateCustomerStatusAsync(id, isActive);
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (result)
            {
                await _activityLog.LogAsync(
                    ActivityModules.Customer,
                    ActivityActions.StatusChange,
                    $"{(isActive ? "Mở khóa" : "Khóa")} tài khoản khách hàng {customer.FullName}",
                    entityType: "ApplicationUser",
                    entityId: id,
                    targetUserId: id);
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            
            return Json(new { success = false, message = "Cập nhật thất bại" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải ít nhất 6 ký tự" });

            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return Json(new { success = false, message = "Không tìm thấy khách hàng" });

            var result = await _customerService.ResetCustomerPasswordAsync(id, newPassword);
            if (result)
            {
                await _activityLog.LogAsync(
                    ActivityModules.Customer,
                    ActivityActions.PasswordReset,
                    $"Admin reset mật khẩu khách hàng {customer.FullName} ({customer.Email})",
                    entityType: "ApplicationUser",
                    entityId: id,
                    targetUserId: id);
                return Json(new { success = true, message = "Đổi mật khẩu thành công" });
            }
            
            return Json(new { success = false, message = "Đổi mật khẩu thất bại" });
        }
    }
}
