using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.DTO
{
    public class StockImportItemDTO
    {
        [Required(ErrorMessage = "SKU là bắt buộc")]
        public string SKU { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int QuantityToImport { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập không được âm")]
        public decimal ImportPrice { get; set; }

        // Các trường dành cho trường hợp tạo mới Sản phẩm
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public decimal? SellingPrice { get; set; } 
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public IFormFile? ImageFile { get; set; } 
        public List<IFormFile>? AdditionalImageFiles { get; set; } 
    }

    public class BulkStockImportDTO
    {
        [Required]
        public int SupplierId { get; set; }
        public string? Notes { get; set; }
        public List<StockImportItemDTO> Items { get; set; } = new List<StockImportItemDTO>();
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
