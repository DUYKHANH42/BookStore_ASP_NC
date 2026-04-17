using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace BookStore.Application.Services
{
    public class CategoriesService
    {
        private readonly ICategoryRepository _repo;
        public CategoriesService(ICategoryRepository repo)
        {
            _repo = repo;
        }
        public async Task<IEnumerable<CategoryDTO>> GetAll()
        {
            var ct = await _repo.GetAllAsync();
            return ct.Select(b => MapToDto(b));
        }
         public async Task<CategoryDTO> GetById(int id)
        {
            var ct = await _repo.GetByIdAsync(id);
            return  MapToDto(ct);
        }
        // viết hàm map đến DTO
        public static CategoryDTO MapToDto(Category ct)
        {
            return new CategoryDTO
            {
                Id = ct.Id,
                Name = ct.Name,
                SubCategories = ct.SubCategories,
            };
        }
    }
}
