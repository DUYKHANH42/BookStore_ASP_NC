using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        // Hàm đặc thù cho Đơn hàng
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);
        Task<Order> GetOrderWithDetailsAsync(int orderId);
    }
}