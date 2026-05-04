using System;
using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class InventoryReceiptDTO
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<InventoryReceiptDetailDTO> Details { get; set; } = new List<InventoryReceiptDetailDTO>();
    }

    public class InventoryReceiptDetailDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal ImportPrice { get; set; }
        public decimal SubTotal => Quantity * ImportPrice;
    }
}
