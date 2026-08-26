namespace BookStore.Domain.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductId { get; set; } // Thay BookId bằng ProductId
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}