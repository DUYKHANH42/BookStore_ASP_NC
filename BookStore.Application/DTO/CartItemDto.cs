namespace BookStore.Application.DTO
{
    public class CartItemDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty; 
        public decimal OriginalPrice { get; set; } 
        public decimal Price { get; set; }         

        public int Quantity { get; set; }
        public decimal SubTotal => Price * Quantity;
    }
}
