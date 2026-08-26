using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class StockHistoryRepository : GenericRepository<StockHistory>, IStockHistoryRepository
    {
        public StockHistoryRepository(BookStoreDbContext context) : base(context) { }

        public async Task<IEnumerable<StockHistory>> GetHistoryByBookIdAsync(int productid)
        {
            return await _context.StockHistories
                .Where(s => s.ProductId == productid)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}