using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetProductWithDetailsAsync(int id);

        Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredPagedAsync(ProductQueryParameters query);

        Task<(IEnumerable<Product> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, ProductQueryParameters query);

        Task<(IEnumerable<Product> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, ProductQueryParameters query);

        Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count);
        Task<IEnumerable<Product>> GetNewArrivalsAsync(int count);
        Task<(IEnumerable<Product> Items, int TotalCount)> SearchProductsAsync(ProductQueryParameters query);
    }
}
