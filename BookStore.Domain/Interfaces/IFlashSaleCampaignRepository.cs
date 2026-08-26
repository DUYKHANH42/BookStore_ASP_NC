using BookStore.Domain.Entities;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IFlashSaleCampaignRepository : IGenericRepository<FlashSaleCampaign>
    {
        Task<FlashSaleCampaign?> GetCampaignWithSalesAsync(int id);
    }
}
