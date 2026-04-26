using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.DTO
{
    public class CartDTO
    {
        public int Id { get; set; }
        public List<CartItemDTO>Items { get; set; } = [];
        public decimal TotalPrice => Items.Sum(x => x.SubTotal);
    }
}
