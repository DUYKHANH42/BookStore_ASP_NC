using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class FlashSaleDTO
    {
        public int Id { get; set; }
        public decimal SalePrice { get; set; }
        public int SaleStock { get; set; }
        public int SoldCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int RemainingSlots { get; set; }
    }

    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? SKU { get; set; }
        
        // Thông tin Flash Sale (nếu có)
        public FlashSaleDTO? FlashSale { get; set; }
        
        // Danh sách hình ảnh
        public List<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
    }

    public class ProductImageDTO
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ProductCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SKU { get; set; }
        // Không còn các trường Sale ở đây
    }
}