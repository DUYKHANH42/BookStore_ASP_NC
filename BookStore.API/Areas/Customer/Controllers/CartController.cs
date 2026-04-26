using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;
    public CartController(CartService cartService) => _cartService = cartService;
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _cartService.GetCartAsync(userId!));
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart(int bookId, int quantity = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _cartService.AddToCartAsync(userId!, bookId, quantity);
        return Ok(result);
    }

    [HttpPut("update-quantity")]
    public async Task<IActionResult> UpdateQuantity(int bookId, int quantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _cartService.UpdateQuantityAsync(userId!, bookId, quantity));
    }

    [HttpDelete("remove/{bookId}")]
    public async Task<IActionResult> RemoveFromCart(int bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _cartService.RemoveFromCartAsync(userId!, bookId));
    }
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _cartService.ClearCartAsync(userId);
        return Ok(result);
    }
}