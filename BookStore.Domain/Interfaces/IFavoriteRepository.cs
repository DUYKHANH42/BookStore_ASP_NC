using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IFavoriteRepository : IGenericRepository<Favorite>
    {
        Task<IEnumerable<Favorite>> GetUserFavoritesAsync(string userId);
        Task<bool> IsFavoritedAsync(string userId, int productId);
        Task<Favorite?> GetFavoriteAsync(string userId, int productId);
    }
}
