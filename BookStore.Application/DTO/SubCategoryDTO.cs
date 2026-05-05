using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO
{
    public class SubCategoryDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục phụ là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục chính")]
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
