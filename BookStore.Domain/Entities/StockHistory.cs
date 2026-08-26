using System;

namespace BookStore.Domain.Entities
{
    public class StockHistory
    {
        public int Id { get; set; }

        public int ProductId { get; set; } 

        public int ChangeQuantity { get; set; } // Số lượng thay đổi

        public string Reason { get; set; } // Lý do thay đổi tồn kho

        public DateTime CreatedAt { get; set; }
        public string ChangedBy { get; set; }
    }
}
