using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IStockHistoryRepository : IGenericRepository<StockHistory>
    {
        Task<IEnumerable<StockHistory>> GetHistoryByBookIdAsync(int bookId);
    }
}