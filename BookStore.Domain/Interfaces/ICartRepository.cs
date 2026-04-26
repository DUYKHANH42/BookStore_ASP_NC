using BookStore.Domain.Entities;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart> GetCartByUserIdAsync(string userId);
        Task<Cart> AddToCartAsync(string userId, int bookId, int quantity);
        Task<Cart> UpdateQuantityAsync(string userId, int bookId, int quantity);
        Task<Cart> RemoveFromCartAsync(string userId, int bookId);
        Task<Cart> ClearCartAsync(string userId);
    }
}