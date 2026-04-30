using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class CategoriesService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoriesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CategoryDTO>> GetAll()
        {
            var categories = await _unitOfWork.Categories.GetAllWithSubCategoriesAsync();
            return categories.Select(b => MapToDto(b));
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
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(int id, CategoryDTO dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return false;

            category.Name = dto.Name;
            await _unitOfWork.Categories.UpdateAsync(category);
            return await _unitOfWork.SaveChangesAsync() > 0;
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
