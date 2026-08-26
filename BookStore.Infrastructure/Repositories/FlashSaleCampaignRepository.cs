using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class FlashSaleCampaignRepository : GenericRepository<FlashSaleCampaign>, IFlashSaleCampaignRepository
    {
        public FlashSaleCampaignRepository(BookStoreDbContext context) : base(context)
        {
        }

        public async Task<FlashSaleCampaign?> GetCampaignWithSalesAsync(int id)
        {
            return await _context.FlashSaleCampaigns
                .Include(c => c.FlashSales)
                    .ThenInclude(s => s.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
