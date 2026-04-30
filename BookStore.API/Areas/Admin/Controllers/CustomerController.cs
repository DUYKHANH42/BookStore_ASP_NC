using BookStore.Application.Services;
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

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index(int page = 1, string search = "", bool? isActive = null)
        {
            int pageSize = 10;
            var result = await _customerService.GetPagedCustomersAsync(page, pageSize, search, isActive);
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
            if (result)
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            
            return Json(new { success = false, message = "Cập nhật thất bại" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải ít nhất 6 ký tự" });

            var result = await _customerService.ResetCustomerPasswordAsync(id, newPassword);
            if (result)
                return Json(new { success = true, message = "Đổi mật khẩu thành công" });
            
            return Json(new { success = false, message = "Đổi mật khẩu thất bại" });
        }
    }
}
