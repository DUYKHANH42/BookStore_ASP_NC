using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class FlashSaleController : Controller
    {
        private readonly FlashSaleService _flashSaleService;
        private readonly IActivityLogService _activityLog;

        public FlashSaleController(FlashSaleService flashSaleService, IActivityLogService activityLog)
        {
            _flashSaleService = flashSaleService;
            _activityLog = activityLog;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string search = "", string status = "", int page = 1, int pageSize = 10)
        {
            var result = await _flashSaleService.GetPagedCampaignsAsync(search, status, page, pageSize);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_SaleListPartial", result);
            }
            
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate([FromBody] FlashSaleCampaignCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            try
            {
                int count = await _flashSaleService.CreateCampaignAsync(dto);
                if (count > 0)
                {
                    await _activityLog.LogAsync(
                        ActivityModules.FlashSale,
                        ActivityActions.Create,
                        $"Tạo chiến dịch Flash Sale '{dto.Name}' với {count} sản phẩm",
                        entityType: "FlashSaleCampaign",
                        entityId: dto.Name);
                    return Json(new { success = true, message = $"Đã tạo thành công chiến dịch sale với {count} sản phẩm!" });
                }
                return Json(new { success = false, message = "Không có sản phẩm nào được thiết lập sale thành công (có thể do trùng lặp thời gian)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            var products = await _flashSaleService.SearchProductsByNameAsync(term);
            // Must return ImageUrl for Select2
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Json(products.Select(p => new {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                imageUrl = !string.IsNullOrEmpty(p.ImageUrl)
                    ? (p.ImageUrl.StartsWith("http") ? p.ImageUrl : $"{baseUrl}/uploads/{p.ImageUrl.TrimStart('/')}")
                    : "https://placehold.co/150x200/e2e8f0/475569?text=No+Image"
            }));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _flashSaleService.ToggleCampaignStatusAsync(id);
            if (result)
            {
                await _activityLog.LogAsync(
                    ActivityModules.FlashSale,
                    ActivityActions.StatusChange,
                    $"Bật/tắt chiến dịch Flash Sale #{id}",
                    entityType: "FlashSaleCampaign",
                    entityId: id.ToString());
            }
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _flashSaleService.DeleteCampaignAsync(id);
                if (result)
                {
                    await _activityLog.LogAsync(
                        ActivityModules.FlashSale,
                        ActivityActions.Delete,
                        $"Xóa chiến dịch Flash Sale #{id}",
                        entityType: "FlashSaleCampaign",
                        entityId: id.ToString());
                }
                return Json(new { success = result, message = result ? "Xóa thành công" : "Không tìm thấy" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaignItems(int id)
        {
            var items = await _flashSaleService.GetCampaignItemsAsync(id);
            return PartialView("_CampaignItemsPartial", items);
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaignForEdit(int id)
        {
            try
            {
                var campaign = await _flashSaleService.GetCampaignForEditAsync(id);
                return Json(new { success = true, data = campaign });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] FlashSaleCampaignEditDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            try
            {
                var result = await _flashSaleService.UpdateCampaignAsync(dto);
                if (result)
                {
                    await _activityLog.LogAsync(
                        ActivityModules.FlashSale,
                        ActivityActions.Update,
                        $"Cập nhật chiến dịch Flash Sale '{dto.Name}' ({dto.Items?.Count ?? 0} SP)",
                        entityType: "FlashSaleCampaign",
                        entityId: dto.Id.ToString());
                }
                return Json(new { success = result, message = result ? "Cập nhật chiến dịch Flash Sale thành công!" : "Không có thay đổi nào được áp dụng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
