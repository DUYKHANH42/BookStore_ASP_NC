using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace BookStore.Domain.Entities
{
    public class InventoryReceipt
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; } = null!;
        
        public string? EmployeeId { get; set; } 
        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }
        public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;

        public virtual ICollection<InventoryReceiptDetail> Details { get; set; } = new List<InventoryReceiptDetail>();
    }

    public enum ReceiptStatus
    {
        Draft,      // Nháp
        Completed,  // Đã nhập kho
        Cancelled   // Đã hủy
    }
}
