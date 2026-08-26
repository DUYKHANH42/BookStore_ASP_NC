using System.Collections.Generic;

namespace BookStore.Application.DTO
{
    public class POSOrderDTO
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public decimal TotalPrice { get; set; }
        public List<POSOrderItemDTO> Items { get; set; } = new List<POSOrderItemDTO>();
    }

    public class POSOrderItemDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
