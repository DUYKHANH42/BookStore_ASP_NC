using BookStore.Application.DTO;
using BookStore.Application.DTOs.Book;
using BookStore.Domain.Common;
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
            return books.Select(MapToDto);
        }

        public async Task<BookDto?> GetById(int id)
        {
            var book = await _repo.GetByIdAsync(id);
            return book != null ? MapToDto(book) : null;
        }

        public async Task<PagedResultDTO<BookDto>> GetAllPaged(BookQueryParameters query)
        {
            var result = await _repo.GetFilteredPagedAsync(query);
            return MapToPagedResult(result, query);
        }

        public async Task<PagedResultDTO<BookDto>> GetByCategoryPaged(int categoryId, BookQueryParameters query)
        {
            var result = await _repo.GetByCategoryPagedAsync(categoryId, query);
            return MapToPagedResult(result, query);
        }

        public async Task<PagedResultDTO<BookDto>> GetBySubCategoryPaged(int subCategoryId, BookQueryParameters query)
        {
            var result = await _repo.GetBySubCategoryPagedAsync(subCategoryId, query);
            return MapToPagedResult(result, query);
        }

        public async Task<IEnumerable<BookDto>> GetNewArrivals(int count)
        {
            var books = await _repo.GetNewArrivalsAsync(count);
            return books.Select(MapToDto);
        }

        private PagedResultDTO<BookDto> MapToPagedResult((IEnumerable<Book> Items, int TotalCount) result, BookQueryParameters query)
        {
            return new PagedResultDTO<BookDto>
            {
                Items = result.Items.Select(MapToDto),
                TotalItems = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize),
                CurrentPage = query.Page,
                PageSize = query.PageSize
            };
        }
        public async Task<PagedResultDTO<BookDto>> Search(BookQueryParameters query)
        {
            var result = await _repo.SearchBooksAsync(query);
            return MapToPagedResult(result, query);
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