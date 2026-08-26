using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface ISubCategoryRepository : IGenericRepository<SubCategory>
    {
        Task<IEnumerable<SubCategory>> GetByCategoryIdAsync(int categoryId);
    }
}