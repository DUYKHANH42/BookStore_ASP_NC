using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.DTO
{
    public class StockImportDTO
    {
        [Required(ErrorMessage = "SKU là bắt buộc")]
        public string SKU { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int QuantityToImport { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập không được âm")]
        public decimal ImportPrice { get; set; }

        public int? SupplierId { get; set; } // Nhà cung cấp
        public string? Notes { get; set; }

        // Các trường dành cho trường hợp tạo mới Sản phẩm
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public decimal? SellingPrice { get; set; } 
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public IFormFile? ImageFile { get; set; } // Upload ảnh thay vì URL
        public List<IFormFile>? AdditionalImageFiles { get; set; } // Thêm nhiều ảnh phụ
    }

    public class StockHistoryDTO
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int ChangeQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }
}
