using BookStore.Application.DTO;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDTO checkoutDto)
        {
            // Lấy UserId từ Token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _orderService.PlaceOrderAsync(userId, checkoutDto);

            if (result == null)
            {
                return BadRequest(new { message = "Giỏ hàng trống hoặc không hợp lệ." });
            }

            return Ok(result);
        }

        // GET: api/orders/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        // GET: api/orders/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Kiểm tra xem người này có phải Admin không
            var isAdmin = User.IsInRole(UserRoles.Admin);

            // 3. Gọi Service lấy đơn hàng
            var order = await _orderService.GetOrderDetailsAsync(id);

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng" });

            // 4. KIỂM TRA BẢO MẬT: 
            // Nếu KHÔNG PHẢI chủ đơn hàng VÀ CŨNG KHÔNG PHẢI Admin thì chặn lại
            if (order.UserId != currentUserId && !isAdmin)
            {
                return Forbid(); // Trả về lỗi 403 Forbidden (Không có quyền xem)
            }

            return Ok(order);
        }
    }
}
