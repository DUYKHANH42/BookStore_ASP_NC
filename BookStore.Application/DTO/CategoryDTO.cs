using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTO
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        public string Name { get; set; } = string.Empty;
        
        public ICollection<SubCategoryDTO> SubCategories { get; set; } = new List<SubCategoryDTO>();
    }
}
