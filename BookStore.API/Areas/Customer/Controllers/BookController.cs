using BookStore.Application.DTO;
using BookStore.Application.DTOs.Book;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
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

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
        {
            var books = await _services.GetAll();
            return Ok(books);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> GetAllBooks([FromQuery] BookQueryParameters query)
        {
            var result = await _services.GetAllPaged(query);
            return Ok(result);
        }

        [HttpGet("category/{id}")]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> GetByCat(int id, [FromQuery] BookQueryParameters query)
        {
            var result = await _services.GetByCategoryPaged(id, query);
            return Ok(result);
        }

        [HttpGet("subcategory/{id}")]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> GetBySub(int id, [FromQuery] BookQueryParameters query)
        {
            var result = await _services.GetBySubCategoryPaged(id, query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBookByID(int id)
        {
            var book = await _services.GetById(id);

            if (book == null)
            {
                return NotFound(new { message = $"Không tìm thấy sách với ID = {id}" });
            }

            return Ok(book);
        }
        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDTO<BookDto>>> SearchBooks([FromQuery] BookQueryParameters query)
        {
            var result = await _services.Search(query);
            return Ok(result);
        }
    }
}