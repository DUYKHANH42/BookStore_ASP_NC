using System;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        ISubCategoryRepository SubCategories { get; }
        IOrderRepository Orders { get; }
        ICartRepository Carts { get; }
        ICustomerRepository Customers { get; }
        IReviewRepository Reviews { get; }
        IStockHistoryRepository StockHistories { get; }
        IShippingAddressRepository ShippingAddresses { get; }
        IInventoryReceiptRepository InventoryReceipts { get; }
        IInventoryReceiptDetailRepository InventoryReceiptDetails { get; }
        ISupplierRepository Suppliers { get; }
        IFlashSaleRepository FlashSales { get; }

        Task<int> SaveChangesAsync();
    }
}