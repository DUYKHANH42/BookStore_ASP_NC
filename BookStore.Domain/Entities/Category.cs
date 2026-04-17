using System.Collections.Generic;

namespace BookStore.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();

    }
}