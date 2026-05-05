using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class ReviewController : Controller
    {
        private readonly ReviewService _reviewService;
        private readonly ProductService _productService;

        public ReviewController(ReviewService reviewService, ProductService productService)
        {
            _reviewService = reviewService;
            _productService = productService;
        }

        public async Task<IActionResult> Index(int? productId)
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            if (productId.HasValue && productId.Value > 0)
            {
                reviews = reviews.Where(r => r.ProductId == productId.Value);
            }

            var products = await _productService.GetAllProductsAsync();
            ViewBag.Products = products;
            ViewBag.SelectedProductId = productId;

            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] AdminReplyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
            }

            var result = await _reviewService.ReplyToReviewAsync(dto);
            if (result) return Json(new { success = true, message = "Đã gửi phản hồi thành công." });
            return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc có lỗi xảy ra." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _reviewService.ToggleReviewStatusAsync(id);
            if (result) return Json(new { success = true, message = "Đã cập nhật trạng thái hiển thị." });
            return Json(new { success = false, message = "Có lỗi xảy ra." });
        }
    }
}
