using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Book)
                   .WithMany(b => b.Images)
                   .HasForeignKey(x => x.BookId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa sách thì xóa hết ảnh
        }
    }
}
