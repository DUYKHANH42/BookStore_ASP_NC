using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class ReviewController : Controller
    {
        private readonly ReviewService _reviewService;

        public ReviewController(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] AdminReplyDTO dto)
        {
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
