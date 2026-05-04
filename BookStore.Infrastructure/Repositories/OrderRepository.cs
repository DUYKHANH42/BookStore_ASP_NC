using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(BookStoreDbContext context) : base(context) { }

        // 1. Lấy chi tiết đơn hàng (Cực kỳ quan trọng: Lấy kèm theo chi tiết và thông tin sách)
        public async Task<Order?> GetOrderByIdWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Lấy thông tin sách của từng item
                .Include(o => o.User) // Lấy thông tin người đặt
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        // 2. Lấy lịch sử mua hàng của một User
        public async Task<IEnumerable<Order>> GetUserOrderHistoryAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();
        }

        // 3. Tìm đơn hàng theo mã (Dùng khi khách tra cứu đơn hàng)
        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        // 4. Xử lý cập nhật trạng thái đơn hàng
        public async Task UpdateStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                // Lưu ý: Không gọi SaveChanges ở đây, để UnitOfWork lo.
            }
        }

        // 5. Lấy đơn hàng theo khoảng thời gian
        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Include(o => o.OrderDetails)
                .ToListAsync();
        }

        public async Task<bool> HasPurchasedProductAsync(string userId, int productId)
        {
            return await _context.Orders
                .AnyAsync(o => o.UserId == userId 
                            && o.Status != OrderStatus.Cancelled 
                            && o.OrderDetails.Any(od => od.ProductId == productId));
        }
    }
}