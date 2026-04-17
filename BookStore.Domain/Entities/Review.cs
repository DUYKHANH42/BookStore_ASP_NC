using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        public string UserId { get; set; }

        public int Rating { get; set; } 

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; } = null!;

    }
}
