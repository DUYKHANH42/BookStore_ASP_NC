using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class FlashSaleController : Controller
    {
        private readonly FlashSaleService _flashSaleService;

        public FlashSaleController(FlashSaleService flashSaleService)
        {
            _flashSaleService = flashSaleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSales(int productId)
        {
            var sales = await _flashSaleService.GetSalesByProductIdAsync(productId);
            return PartialView("_ProductSalesPartial", sales);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FlashSaleCreateDTO dto)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var result = await _flashSaleService.CreateFlashSaleAsync(dto);
                if (result) return Json(new { success = true, message = "Thêm Flash Sale thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi tạo Flash Sale" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _flashSaleService.ToggleSaleStatusAsync(id);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _flashSaleService.DeleteSaleAsync(id);
                return Json(new { success = result, message = result ? "Xóa thành công" : "Không tìm thấy" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
