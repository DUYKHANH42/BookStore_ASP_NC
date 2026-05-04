namespace BookStore.Application.DTO
{
    public class OrderItemDetailDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // BookTitle -> ProductName
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal => Price * Quantity;
    }
}
