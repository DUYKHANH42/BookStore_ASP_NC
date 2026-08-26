using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class Cart
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
        public ApplicationUser User { get; set; } = null!;

    }
}
