using BookStore.Application.DTO;
using BookStore.Application.DTOs.Book; // Đảm bảo đã using namespace của DTO
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class BookService
    {
        private readonly IBookRepository _repo;

        public BookService(IBookRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<BookDto>> GetAll()
        {
            var books = await _repo.GetAllAsync();
            return books.Select(b => MapToDto(b));
        }

        public async Task<BookDto?> GetById(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            if (book == null) return null;

            return MapToDto(book);
        }

        public async Task<PagedResultDTO<BookDto>> GetAllPaged(int page, int pageSize)
        => MapToPagedResult(await _repo.GetPagedAsync(page, pageSize), page, pageSize);

        public async Task<PagedResultDTO<BookDto>> GetByCategoryPaged(int categoryId, int page, int pageSize)
            => MapToPagedResult(await _repo.GetByCategoryPagedAsync(categoryId, page, pageSize), page, pageSize);

        public async Task<PagedResultDTO<BookDto>> GetBySubCategoryPaged(int subCategoryId, int page, int pageSize)
            => MapToPagedResult(await _repo.GetBySubCategoryPagedAsync(subCategoryId, page, pageSize), page, pageSize);

        private PagedResultDTO<BookDto> MapToPagedResult((IEnumerable<Book> Items, int TotalCount) result, int page, int pageSize)
        {
            return new PagedResultDTO<BookDto>
            {
                Items = result.Items.Select(b => MapToDto(b)),
                TotalItems = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        private static BookDto MapToDto(Book b)
        {
            return new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Price = b.Price,
                Description = b.Description,
                Quantity = b.Quantity,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                ImageUrl = b.ImageUrl,
                CategoryId = b.CategoryId,
                IsActive = b.IsActive,
                SKU = b.SKU,
                SubCategoryId = b.SubCategoryId,

                DiscountPrice = b.DiscountPrice,
                SaleEndDate = b.SaleEndDate,
                IsFlashSale = b.IsFlashSale,
                SaleSoldCount = b.SaleSoldCount,
                SaleStock = b.SaleStock,

                CategoryName = b.Category?.Name,
                SubCategoryName = b.SubCategory?.Name
            };
        }
    }
}