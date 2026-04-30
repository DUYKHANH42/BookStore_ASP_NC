using System;

namespace BookStore.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        public int Rating { get; set; } // 1-5 sao

        public string? Comment { get; set; }

        public string? AdminReply { get; set; } // Phản hồi từ Admin

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true; // Cho phép ẩn đánh giá xấu/vi phạm
    }
}
