using System;

namespace BookStore.Application.DTOs.Book
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
        public string? SKU { get; set; }
        public int? SubCategoryId { get; set; }

        // Flash Sale Fields (Khớp với Angular)
        public decimal? DiscountPrice { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public bool? IsFlashSale { get; set; }
        public int? SaleSoldCount { get; set; }
        public int? SaleStock { get; set; }

        // Mẹo: Nên thêm 2 trường này để Angular hiển thị tên loại luôn, đỡ phải tìm
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
    }
}