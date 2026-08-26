using System;

namespace BookStore.Domain.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; } // Đổi từ BookId sang ProductId
        public virtual Product Product { get; set; } = null!; // Navigation property

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; } // Ảnh đại diện chính hay không
        public int DisplayOrder { get; set; } // Thứ tự hiển thị
    }
}
