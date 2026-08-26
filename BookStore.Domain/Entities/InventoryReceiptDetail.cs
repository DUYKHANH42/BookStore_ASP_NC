using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Domain.Entities
{
    public class InventoryReceiptDetail
    {
        public int Id { get; set; }
        public int InventoryReceiptId { get; set; }
        public virtual InventoryReceipt InventoryReceipt { get; set; } = null!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportPrice { get; set; } // Giá nhập của từng sản phẩm
    }
}

