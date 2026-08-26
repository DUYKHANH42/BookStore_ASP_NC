using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Product) // Book -> Product
                   .WithMany(b => b.Images)
                   .HasForeignKey(x => x.ProductId) // BookId -> ProductId
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
