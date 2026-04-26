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

            builder.HasIndex(f => new { f.UserId, f.BookId }).IsUnique();

            builder.HasOne(f => f.User)
                   .WithMany() 
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Cascade); 

            // 4. Cấu hình quan hệ với Book
            builder.HasOne(f => f.Book)
                   .WithMany() // Một Book có thể được nhiều người thích
                   .HasForeignKey(f => f.BookId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Book thì xóa luôn Favorite
        }
    }
}