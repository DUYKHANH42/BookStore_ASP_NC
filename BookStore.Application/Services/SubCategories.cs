using BookStore.Application.DTO;
using BookStore.Application.DTOs.Book;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class SubCategories
    {
        private readonly ISubCategoryRepository _repo;
        public SubCategories(ISubCategoryRepository repo)
        {
            _repo = repo;
        }
        public async Task<IEnumerable<SubCategoryDTO>> GetAll()
        {
            var sc = await _repo.GetAllAsync();
            return sc.Select(b => MapToDto(b));
        }
        public async Task<SubCategoryDTO> GetById(int id)
        {
            var sc = await _repo.GetByIdAsync(id);
            return MapToDto(sc);

        }
        private static SubCategoryDTO MapToDto(SubCategory sc)
        {
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
