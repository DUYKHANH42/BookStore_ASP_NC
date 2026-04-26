using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public virtual Book Book { get; set; } = null!; // Navigation property

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; } // Ảnh đại diện chính hay không
        public int DisplayOrder { get; set; } // Thứ tự hiển thị
    }
}
