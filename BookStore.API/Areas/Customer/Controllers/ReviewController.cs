using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewService _reviewService;

        public ReviewController(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // POST: api/reviews
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] CreateReviewDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reviewService.SubmitReviewAsync(userId, dto);
            if (result.success)
            {
                return Ok(new { message = result.message });
            }
            return BadRequest(new { message = result.message });
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId);
            return Ok(reviews);
        }

        [HttpGet("check-eligibility/{productId}")]
        public async Task<IActionResult> CheckEligibility(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Ok(new { canReview = false });

            // Cần thêm phương thức này vào ReviewService
            var canReview = await _reviewService.CanUserReviewProductAsync(userId, productId);
            return Ok(new { canReview });
        }
    }
}
