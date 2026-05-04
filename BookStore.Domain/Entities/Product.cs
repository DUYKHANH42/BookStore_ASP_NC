using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace BookStore.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } 

        public int Quantity { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public string? SKU { get; set; } 
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();
    }
}
