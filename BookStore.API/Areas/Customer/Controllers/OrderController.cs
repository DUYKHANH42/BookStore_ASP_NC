using BookStore.Application.DTO;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

using System.Collections.Generic;
using System;
using System.Linq;

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Khách hàng";

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _orderService.ProcessCheckoutAsync(userId, checkoutDto, userName);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(new { message = result.Message });
        }

        [AllowAnonymous]
        [HttpPost("zalopay-callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackDTO cbData)
        {
            var dataStr = cbData.data;
            var requestMac = cbData.mac;

            var callbackResult = await _orderService.ProcessZaloPayCallbackAsync(dataStr, requestMac);
            
            var result = new Dictionary<string, object>();
            if (callbackResult.Success)
            {
                result["return_code"] = 1;
                result["return_message"] = "success";
            }
            else
            {
                result["return_code"] = (callbackResult.Message == "mac not equal") ? -1 : 0;
                result["return_message"] = callbackResult.Message;
            }

            return Ok(result);
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _orderService.GetUserOrdersPagedAsync(userId, page, pageSize);
            return Ok(result);
        }

        // POST: api/orders/1/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cancelResult = await _orderService.CancelOrderForUserAsync(id, userId);
            
            if (cancelResult.Success)
            {
                return Ok(new { success = cancelResult.Success, message = cancelResult.Message });
            }
            
            return BadRequest(new { success = cancelResult.Success, message = cancelResult.Message });
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
