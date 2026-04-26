using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IFavoriteRepository : IGenericRepository<Favorite>
    {
        Task<IEnumerable<Favorite>> GetUserFavoritesAsync(string userId);
        Task<bool> IsFavoritedAsync(string userId, int bookId);
        Task<Favorite?> GetFavoriteAsync(string userId, int bookId);
    }
}
