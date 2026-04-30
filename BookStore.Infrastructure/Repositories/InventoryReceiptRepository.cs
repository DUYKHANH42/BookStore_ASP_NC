using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;

namespace BookStore.Infrastructure.Repositories
{
    public class InventoryReceiptRepository : GenericRepository<InventoryReceipt>, IInventoryReceiptRepository
    {
        public InventoryReceiptRepository(BookStoreDbContext context) : base(context)
        {
        }
    }
}
