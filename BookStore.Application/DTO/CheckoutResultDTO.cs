namespace BookStore.Application.DTO
{
    public class CheckoutResultDTO
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public OrderDTO? Order { get; set; }
        public string? PaymentUrl { get; set; }
    }
}
