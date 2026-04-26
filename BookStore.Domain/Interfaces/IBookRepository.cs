using BookStore.Domain.Common;
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
        Task<Book?> GetBookWithDetailsAsync(int id);

        Task<(IEnumerable<Book> Items, int TotalCount)> GetFilteredPagedAsync(BookQueryParameters query);

        Task<(IEnumerable<Book> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, BookQueryParameters query);

        Task<(IEnumerable<Book> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, BookQueryParameters query);

        Task<IEnumerable<Book>> GetRelatedBooksAsync(int bookId, int count);
        Task<IEnumerable<Book>> GetNewArrivalsAsync(int count);
        Task<(IEnumerable<Book> Items, int TotalCount)> SearchBooksAsync(BookQueryParameters query);

    }
}
