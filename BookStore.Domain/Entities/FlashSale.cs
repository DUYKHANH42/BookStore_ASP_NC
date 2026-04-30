using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Domain.Entities
{
    public class FlashSale
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        public int SaleStock { get; set; } // Tổng số lượng suất sale
        public int SoldCount { get; set; } // Số lượng đã bán được giá sale

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public int RemainingSlots => Math.Max(0, SaleStock - SoldCount);

        public bool IsValid => IsActive && DateTime.Now >= StartTime && DateTime.Now <= EndTime && RemainingSlots > 0;
    }
}
