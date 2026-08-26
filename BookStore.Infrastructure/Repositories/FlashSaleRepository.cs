using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class FlashSaleRepository : GenericRepository<FlashSale>, IFlashSaleRepository
    {
        public FlashSaleRepository(BookStoreDbContext context) : base(context) { }

        public async Task<FlashSale?> GetActiveSaleByProductIdAsync(int productId)
        {
            var now = TimeHelper.GetVnTime();
            return await _context.FlashSales
                .Include(f => f.FlashSaleCampaign)
                .Where(s => s.ProductId == productId && 
                            s.FlashSaleCampaign.IsActive && 
                            s.FlashSaleCampaign.StartTime <= now && 
                            s.FlashSaleCampaign.EndTime >= now && 
                            s.SoldCount < s.SaleStock)
                .OrderByDescending(s => s.FlashSaleCampaign.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FlashSale>> GetSalesByProductIdAsync(int productId)
        {
            return await _context.FlashSales
                .Include(f => f.FlashSaleCampaign)
                .Where(s => s.ProductId == productId)
                .OrderByDescending(s => s.FlashSaleCampaign.StartTime)
                .ToListAsync();
        }
    }
}

