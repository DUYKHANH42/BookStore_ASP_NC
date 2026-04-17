using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class Book
    {
        public int Id { get; set; } // Primary key(mã sách)
        public string Title { get; set; } // Title of the book(tên sách)
        public string Author { get; set; } // Author of the book(tác giả)
        public string Description { get; set; } // Description of the book(mô tả sách)
        public decimal Price { get; set; } // Price of the book(giá sách)
        public int Quantity { get; set; } // Quantity of the book in stock(số lượng sách trong kho)
        public DateTime CreatedAt { get; set; } // Date and time when the book was created(thời gian tạo sách)
        public DateTime? UpdatedAt { get; set; } // Date and time when the book was last updated(thời gian cập nhật sách)
        public string ImageUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; } // Foreign key to Category(mã thể loại)
        public Category Category { get; set; } = null!; // Navigation property to Category(thể loại)
        public bool IsActive { get; set; } = true; // Trạng thái hoạt động của sách
        public string? SKU { get; set; } // Stock Keeping Unit - mã định danh duy nhất cho sách, có thể dùng để quản lý tồn kho và bán hàng
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public bool IsFlashSale { get; set; } = false;
        public int? SaleSoldCount { get; set; }
        public int? SaleStock { get; set; }

    }
}
