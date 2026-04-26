using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
    {
        public FavoriteRepository(BookStoreDbContext context) : base(context) { }

        public async Task<IEnumerable<Favorite>> GetUserFavoritesAsync(string userId)
        {
            return await _context.Favorites
                .Include(f => f.Book) // Lấy kèm thông tin sách để hiển thị
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> IsFavoritedAsync(string userId, int bookId)
        {
            return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId);
        }

        public async Task<Favorite?> GetFavoriteAsync(string userId, int bookId)
        {
            return await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);
        }
    }
}
