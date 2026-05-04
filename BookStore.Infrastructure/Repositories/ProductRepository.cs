using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(BookStoreDbContext context) : base(context) { }

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _context.Products
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Images)
                .Include(b => b.FlashSales) // THÊM
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredPagedAsync(ProductQueryParameters query)
        {
            var result = _context.Products
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.FlashSales) // THÊM
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.Search))
            {
                result = result.Where(b => b.Name.Contains(query.Search) || b.Brand.Contains(query.Search));
            }

            if (query.MinPrice.HasValue)
                result = result.Where(b => b.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                result = result.Where(b => b.Price <= query.MaxPrice.Value);

            if (query.IsActive.HasValue)
                result = result.Where(b => b.IsActive == query.IsActive.Value);

            if (query.IsFlashSale)
            {
                var now = DateTime.Now;
                // Lọc những sản phẩm có Flash Sale đang hiệu lực
                result = result.Where(b => b.FlashSales.Any(s => 
                    s.IsActive && s.StartTime <= now && s.EndTime >= now && s.SoldCount < s.SaleStock));
            }

            // Lọc theo SubCategory trước (ưu tiên cao hơn)
            if (query.SubCategoryId.HasValue && query.SubCategoryId.Value > 0)
            {
                result = result.Where(b => b.SubCategoryId == query.SubCategoryId.Value);
            }
            // Nếu không có SubCategory thì mới lọc theo Category
            else if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
            {
                result = result.Where(b => b.CategoryId == query.CategoryId.Value);
            }

            // Sorting
            result = query.SortBy switch
            {
                "price_asc" => result.OrderBy(b => b.Price),
                "price_desc" => result.OrderByDescending(b => b.Price),
                "newest" => result.OrderByDescending(b => b.CreatedAt),
                "oldest" => result.OrderBy(b => b.CreatedAt),
                _ => result.OrderByDescending(b => b.CreatedAt)
            };

            var totalCount = await result.CountAsync();
            var items = await result.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, ProductQueryParameters query)
        {
            var result = _context.Products.Where(b => b.CategoryId == categoryId);
            return await GetPagedResultAsync(result, query);
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetBySubCategoryPagedAsync(int subCategoryId, ProductQueryParameters query)
        {
            var result = _context.Products.Where(b => b.SubCategoryId == subCategoryId);
            return await GetPagedResultAsync(result, query);
        }

        public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Enumerable.Empty<Product>();

            return await _context.Products
                .Where(b => b.CategoryId == product.CategoryId && b.Id != productId)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetNewArrivalsAsync(int count)
        {
            return await _context.Products
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchProductsAsync(ProductQueryParameters query)
        {
            return await GetFilteredPagedAsync(query);
        }
        
        public async Task<Product?> GetBySKUAsync(string sku)
        {
            return await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        private async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedResultAsync(IQueryable<Product> query, ProductQueryParameters parameters)
        {
            var totalCount = await query.CountAsync();
            var items = await query.Skip((parameters.PageNumber - 1) * parameters.PageSize).Take(parameters.PageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}