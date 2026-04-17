using System;
using System.Collections.Generic;

namespace BookStore.Domain.Entities
{
    public class SubCategory
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}