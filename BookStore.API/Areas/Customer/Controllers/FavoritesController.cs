using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize] 
[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly FavoriteService _service;
    public FavoritesController(FavoriteService service) => _service = service;

    [HttpPost("toggle/{bookId}")]
    public async Task<IActionResult> Toggle(int bookId)
    {
        // Lấy UserId từ Token JWT đã đăng nhập
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _service.ToggleFavoriteAsync(userId, bookId);
        return Ok(new { isFavorited = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        return Ok(await _service.GetUserFavorites(userId));
    }
}