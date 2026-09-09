using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasIndex(o => o.Status).HasDatabaseName("IX_Orders_Status");
            builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");
            builder.HasIndex(o => o.OrderNumber).HasDatabaseName("IX_Orders_OrderNumber");
            builder.HasIndex(o => new { o.Status, o.CreatedAt }).HasDatabaseName("IX_Orders_Status_CreatedAt");
            builder.HasIndex(o => o.UserId).HasDatabaseName("IX_Orders_UserId");

            builder.HasMany(o => o.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
