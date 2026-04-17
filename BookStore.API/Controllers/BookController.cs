using BookStore.Application.DTO;
using BookStore.Application.DTOs.Book;
using BookStore.Application.Services;
using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks()
        {
            var books = await _services.GetAll();
            return Ok(books);
        }
        [HttpGet("category/{id}")]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> GetByCat(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    => Ok(await _services.GetByCategoryPaged(id, page, pageSize));

        [HttpGet("subcategory/{id}")]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> GetBySub(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _services.GetBySubCategoryPaged(id, page, pageSize));

    }
}
