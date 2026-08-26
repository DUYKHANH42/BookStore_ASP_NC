using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.HasKey(f => f.Id);

            builder.HasIndex(f => new { f.UserId, f.ProductId }).IsUnique(); // BookId -> ProductId

            builder.HasOne(f => f.User)
                   .WithMany() 
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Cascade); 

            builder.HasOne(f => f.Product) // Book -> Product
                   .WithMany()
                   .HasForeignKey(f => f.ProductId) // BookId -> ProductId
                   .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}