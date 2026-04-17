using BookStore.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BookController : ControllerBase
    {
       private readonly BookService _services;
        public BookController(BookService services)
        {
            _services = services;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _services.GetAll();
            return Ok(books);
        }
    }
}
