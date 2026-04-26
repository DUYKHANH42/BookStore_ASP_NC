using System;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBookRepository Books { get; }
        ICategoryRepository Categories { get; }
        ISubCategoryRepository SubCategories { get; }
        IOrderRepository Orders { get; }
        ICartRepository Carts { get; }
        ICustomerRepository Customers { get; }
        IReviewRepository Reviews { get; }
        IStockHistoryRepository StockHistories { get; }
        IShippingAddressRepository ShippingAddresses { get; }

        Task<int> SaveChangesAsync(); // Hàm quan trọng nhất để thực thi Transaction
    }
}