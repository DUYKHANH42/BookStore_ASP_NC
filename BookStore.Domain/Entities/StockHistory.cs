using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class StockHistory
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public int ChangeQuantity { get; set; } // Số lượng thay đổi (có thể là dương hoặc âm)

        public string Reason { get; set; } // Lý do thay đổi tồn kho (ví dụ: "Nhập hàng", "Bán hàng", "Hủy đơn hàng", v.v.)

        public DateTime CreatedAt { get; set; }
        public string ChangedBy { get; set; }
    }
}
