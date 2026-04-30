using BookStore.Domain.Entities;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IFlashSaleRepository : IGenericRepository<FlashSale>
    {
        Task<FlashSale?> GetActiveSaleByProductIdAsync(int productId);
        Task<System.Collections.Generic.IEnumerable<FlashSale>> GetSalesByProductIdAsync(int productId);
    }
}
