using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class CategoriesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private const string CATEGORY_CACHE_KEY = "categories_all";

        public CategoriesService(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<IEnumerable<CategoryDTO>> GetAll()
        {
            if (!_cache.TryGetValue(CATEGORY_CACHE_KEY, out IEnumerable<CategoryDTO>? cachedCategories))
            {
                var categories = await _unitOfWork.Categories.GetAllWithSubCategoriesAsync();
                cachedCategories = categories.Select(b => MapToDto(b)).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(24))
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(CATEGORY_CACHE_KEY, cachedCategories, cacheEntryOptions);
            }

            return cachedCategories ?? Enumerable.Empty<CategoryDTO>();
        }

        public async Task<CategoryDTO> GetById(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            return MapToDto(category);
        }

        public async Task<bool> CreateAsync(CategoryDTO dto)
        {
            var category = new Category
            {
                Name = dto.Name
            };
            await _unitOfWork.Categories.AddAsync(category);
            var success = await _unitOfWork.SaveChangesAsync() > 0;
            if (success) _cache.Remove(CATEGORY_CACHE_KEY);
            return success;
        }

        public async Task<bool> UpdateAsync(int id, CategoryDTO dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return false;

            category.Name = dto.Name;
            await _unitOfWork.Categories.UpdateAsync(category);
            var success = await _unitOfWork.SaveChangesAsync() > 0;
            if (success) _cache.Remove(CATEGORY_CACHE_KEY);
            return success;
        }

        public async Task<string> DeleteAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return "Không tìm thấy danh mục";

            var subCategories = await _unitOfWork.SubCategories.GetAllAsync();
            var hasSubCategories = subCategories.Any(s => s.CategoryId == id);
            if (hasSubCategories) return "Không thể xóa danh mục này vì vẫn còn danh mục phụ bên trong";

            var products = await _unitOfWork.Products.GetAllAsync();
            var hasProducts = products.Any(p => p.CategoryId == id);
            if (hasProducts) return "Không thể xóa danh mục này vì vẫn còn sản phẩm đang sử dụng";

            await _unitOfWork.Categories.DeleteAsync(id);
            var success = await _unitOfWork.SaveChangesAsync() > 0;
            if (success) _cache.Remove(CATEGORY_CACHE_KEY);
            return success ? "success" : "Lỗi khi xóa dữ liệu";
        }

        public static CategoryDTO MapToDto(Category ct)
        {
            if (ct == null) return null;
            return new CategoryDTO
            {
                Id = ct.Id,
                Name = ct.Name,
                SubCategories = ct.SubCategories?.Select(sc => new SubCategoryDTO
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    CategoryId = sc.CategoryId,
                    CreatedAt = sc.CreatedAt,
                    UpdatedAt = sc.UpdatedAt
                }).ToList() ?? new List<SubCategoryDTO>()
            };
        }
    }
}
