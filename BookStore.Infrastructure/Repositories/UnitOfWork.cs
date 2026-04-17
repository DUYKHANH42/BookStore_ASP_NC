using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Repositories;
using System.Threading.Tasks;

public class UnitOfWork : IUnitOfWork
{
    private readonly BookStoreDbContext _context;

    public IBookRepository Books { get; }
    public ICategoryRepository Categories { get; }
    public ISubCategoryRepository SubCategories { get; }
    public IOrderRepository Orders { get; }
    public ICartRepository Carts { get; }
    public ICustomerRepository Customers { get; }
    public IReviewRepository Reviews { get; }
    public IStockHistoryRepository StockHistories { get; }

    public UnitOfWork(BookStoreDbContext context)
    {
        _context = context;
        Books = new BookRepository(_context);
        Categories = new CategoryRepository(_context);
        SubCategories = new SubCategoryRepository(_context);
        Orders = new OrderRepository(_context);
        Carts = new CartRepository(_context);
        Customers = new CustomerRepository(_context);
        Reviews = new ReviewRepository(_context);
        StockHistories = new StockHistoryRepository(_context);
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}