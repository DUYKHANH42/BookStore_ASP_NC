using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IEnumerable<Review>> GetReviewsByProductIdAsync(int productId);
        Task<Review?> GetReviewByUserAndProductAsync(string userId, int productId);
        Task<IEnumerable<Review>> GetAllWithIncludeAsync();
    }
}