using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ApplicationUser User { get; set; } = null!;

    }
}
