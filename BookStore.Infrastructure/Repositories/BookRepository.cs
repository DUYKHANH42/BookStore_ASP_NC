using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class BookRepository : GenericRepository<Book>, IBookRepository
    {
        public BookRepository(BookStoreDbContext context) : base(context) { }

        public async Task<IEnumerable<Book>> GetBooksByCategoryIdAsync(int categoryId)
        {
            return await _context.Books
                .Where(b => b.CategoryId == categoryId)
                 .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> SearchBooksAsync(BookQueryParameters query)
        {
            var dbQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .AsNoTracking();

            // ApplyFilters đã có logic lọc theo SearchTerm, MinPrice, MaxPrice và SortBy
            dbQuery = ApplyFilters(dbQuery, query);

            var totalCount = await dbQuery.CountAsync();
            var items = await dbQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<IEnumerable<Book>> GetNewArrivalsAsync(int count)
        {
            return await _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public new async Task<IEnumerable<Book>> GetAllAsync()
        {
            return await _context.Books
                 .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .ToListAsync();
        }

        public new async Task<(IEnumerable<Book> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _context.Books.AsNoTracking();
            return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(), await query.CountAsync());
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, int page, int pageSize)
        {
            var query = _context.Books.Where(b => b.CategoryId == categoryId).AsNoTracking();
            return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(), await query.CountAsync());
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, int page, int pageSize)
        {
            var query = _context.Books.Where(b => b.SubCategoryId == subCategoryId).AsNoTracking();
            return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(), await query.CountAsync());
        }

        public new async Task<Book?> GetByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Book?> GetBookWithDetailsAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> GetFilteredPagedAsync(BookQueryParameters query)
        {
            var result = _context.Books.Include(b => b.Category).AsNoTracking().AsQueryable();
            result = ApplyFilters(result, query);
            var totalCount = await result.CountAsync();
            var items = await result.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, BookQueryParameters query)
        {
            var result = _context.Books.Include(b => b.Category).Where(b => b.CategoryId == categoryId).AsNoTracking().AsQueryable();
            result = ApplyFilters(result, query);
            var totalCount = await result.CountAsync();
            var items = await result.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<(IEnumerable<Book> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, BookQueryParameters query)
        {
            var result = _context.Books.Include(b => b.Category).Where(b => b.SubCategoryId == subCategoryId).AsNoTracking().AsQueryable();
            result = ApplyFilters(result, query);
            var totalCount = await result.CountAsync();
            var items = await result.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, int count)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return new List<Book>();
            return await _context.Books
                .Where(b => b.CategoryId == book.CategoryId && b.Id != bookId && b.IsActive)
                .Take(count)
                .ToListAsync();
        }

        private IQueryable<Book> ApplyFilters(IQueryable<Book> query, BookQueryParameters parameters)
        {
            if (parameters.MinPrice.HasValue) query = query.Where(b => b.Price >= parameters.MinPrice);
            if (parameters.MaxPrice.HasValue) query = query.Where(b => b.Price <= parameters.MaxPrice);
            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                var search = parameters.SearchTerm.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(search) || b.Author.ToLower().Contains(search));
            }
            query = parameters.SortBy switch
            {
                "price_asc" => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "newest" => query.OrderByDescending(b => b.CreatedAt),
                _ => query.OrderByDescending(b => b.Id)
            };
            return query;
        }
    }
}