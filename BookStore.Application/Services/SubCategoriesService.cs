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
    public class SubCategoriesService
    {
        private readonly IUnitOfWork _unitOfWork;
        public SubCategoriesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SubCategoryDTO>> GetAll()
        {
            var subCategories = await _unitOfWork.SubCategories.GetAllAsync();
            return subCategories.Select(b => MapToDto(b));
        }

        public async Task<IEnumerable<SubCategoryDTO>> GetSubCategoriesByCategoryIdAsync(int categoryId)
        {
            var subCategories = await _unitOfWork.SubCategories.GetAllAsync();
            return subCategories
                .Where(s => s.CategoryId == categoryId)
                .Select(s => MapToDto(s));
        }

        public async Task<SubCategoryDTO> GetById(int id)
        {
            var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
            return MapToDto(subCategory);
        }

        public async Task<bool> CreateAsync(SubCategoryDTO dto)
        {
            var subCategory = new SubCategory
            {
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.SubCategories.AddAsync(subCategory);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(int id, SubCategoryDTO dto)
        {
            var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
            if (subCategory == null) return false;

            subCategory.Name = dto.Name;
            subCategory.CategoryId = dto.CategoryId;
            subCategory.UpdatedAt = DateTime.Now;

            await _unitOfWork.SubCategories.UpdateAsync(subCategory);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<string> DeleteAsync(int id)
        {
            var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
            if (subCategory == null) return "Không tìm thấy danh mục phụ";

            // Kiểm tra sản phẩm
            var products = await _unitOfWork.Products.GetAllAsync();
            var hasProducts = products.Any(p => p.SubCategoryId == id);
            if (hasProducts) return "Không thể xóa vì vẫn còn sản phẩm thuộc danh mục phụ này";
            await _unitOfWork.SubCategories.DeleteAsync(id);
            var success = await _unitOfWork.SaveChangesAsync() > 0;
            return success ? "success" : "Lỗi khi xóa dữ liệu";
        }

        private static SubCategoryDTO MapToDto(SubCategory sc)
        {
            if (sc == null) return null;
            return new SubCategoryDTO
            {
                Id = sc.Id,
                Name = sc.Name,
                CategoryId = sc.CategoryId,
                CreatedAt = sc.CreatedAt,
                UpdatedAt = sc.UpdatedAt,
            };
        }
    }
}
