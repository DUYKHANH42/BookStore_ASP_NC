using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;

namespace BookStore.Infrastructure.Repositories
{
    public class InventoryReceiptDetailRepository : GenericRepository<InventoryReceiptDetail>, IInventoryReceiptDetailRepository
    {
        public InventoryReceiptDetailRepository(BookStoreDbContext context) : base(context)
        {
        }
    }
}
