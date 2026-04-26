using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO
{
    public class CartItemDTO
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; } 
        public decimal Price { get; set; }         

        public int Quantity { get; set; }
        public decimal SubTotal => Price * Quantity;
    }

}
