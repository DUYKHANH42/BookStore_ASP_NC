using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(p => p.RowVersion)
                   .IsRowVersion();

            builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
            builder.HasIndex(p => p.SubCategoryId).HasDatabaseName("IX_Products_SubCategoryId");
            builder.HasIndex(p => p.IsActive).HasDatabaseName("IX_Products_IsActive");
            builder.HasIndex(p => new { p.IsActive, p.CreatedAt }).HasDatabaseName("IX_Products_IsActive_CreatedAt");
        }
    }
}
