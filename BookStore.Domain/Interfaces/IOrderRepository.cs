using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        // 1. Lấy chi tiết đơn hàng (Kèm OrderDetails và thông tin Book)
        Task<Order?> GetOrderByIdWithDetailsAsync(int orderId);

        // 2. Lấy danh sách đơn hàng của một người dùng
        Task<IEnumerable<Order>> GetUserOrderHistoryAsync(string userId);

        // 3. Lấy đơn hàng theo mã đơn hàng (OrderNumber)
        Task<Order?> GetByOrderNumberAsync(string orderNumber);

        // 4. Xử lý thay đổi trạng thái đơn hàng (Xử lý dữ liệu ở Infra)
        Task UpdateStatusAsync(int orderId, OrderStatus status);
    }
}