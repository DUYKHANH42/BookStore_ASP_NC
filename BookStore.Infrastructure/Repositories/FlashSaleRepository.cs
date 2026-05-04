using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class FlashSaleRepository : GenericRepository<FlashSale>, IFlashSaleRepository
    {
        public FlashSaleRepository(BookStoreDbContext context) : base(context) { }

        public async Task<FlashSale?> GetActiveSaleByProductIdAsync(int productId)
        {
            var now = DateTime.Now;
            return await _context.FlashSales
                .Where(s => s.ProductId == productId && s.IsActive && s.StartTime <= now && s.EndTime >= now && s.SoldCount < s.SaleStock)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<FlashSale>> GetSalesByProductIdAsync(int productId)
        {
            return await _context.FlashSales
                .Where(s => s.ProductId == productId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }
    }
}
