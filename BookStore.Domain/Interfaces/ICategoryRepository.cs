using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category> GetCategoryWithSubCategoriesAsync(int id);
        Task<IEnumerable<Category>> GetAllWithSubCategoriesAsync();
    }
}