using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations
{
    public class StockHistoryConfiguration : IEntityTypeConfiguration<StockHistory>
    {
        public void Configure(EntityTypeBuilder<StockHistory> builder)
        {
            builder.HasIndex(s => s.ProductId).HasDatabaseName("IX_StockHistory_ProductId");
            builder.HasIndex(s => s.CreatedAt).HasDatabaseName("IX_StockHistory_CreatedAt");
        }
    }
}
