using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IBookRepository : IGenericRepository<Book>
    {
        
        Task<IEnumerable<Book>> GetBooksByCategoryIdAsync(int categoryId);
        Task<(IEnumerable<Book> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task<(IEnumerable<Book> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, int page, int pageSize);
        Task<(IEnumerable<Book> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, int page, int pageSize);
        Task<IEnumerable<Book>> SearchBooksAsync(string keyword);

    }
}
