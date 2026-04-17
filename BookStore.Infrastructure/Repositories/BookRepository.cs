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
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string keyword)
        {
            return await _context.Books
                .Where(b => b.Title.Contains(keyword) || b.Author.Contains(keyword))
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetNewArrivalsAsync(int count)
        {
            return await _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}