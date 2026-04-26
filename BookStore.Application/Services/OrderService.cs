using BookStore.Application.DTO;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class OrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OrderDTO?> PlaceOrderAsync(string userId, CheckoutDTO checkoutDto)
        {
            var cart = await _unitOfWork.Carts.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any()) return null;

            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                UserId = userId,
                ShippingName = checkoutDto.ShippingName,
                ShippingPhone = checkoutDto.ShippingPhone,
                ShippingAddress = checkoutDto.ShippingAddress,
                PaymentMethod = checkoutDto.PaymentMethod,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.Now
            };

            decimal total = 0;
            foreach (var item in cart.Items)
            {
                // Logic lấy giá thực tế: Nếu FlashSale thì lấy DiscountPrice, không thì lấy Price
                decimal actualPrice = (item.Book.IsFlashSale && item.Book.DiscountPrice.HasValue)
                                      ? item.Book.DiscountPrice.Value
                                      : item.Book.Price;

                var orderDetail = new OrderDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    Price = actualPrice // Chốt giá đã giảm vào đơn hàng
                };
                order.OrderDetails.Add(orderDetail);
                total += actualPrice * item.Quantity;
            }

            order.TotalPrice = total;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.Carts.ClearCartAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt
            };
        }

        public async Task<IEnumerable<OrderDTO>> GetUserOrdersAsync(string userId)
        {
            var orders = await _unitOfWork.Orders.GetUserOrderHistoryAsync(userId);
            return orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                TotalPrice = o.TotalPrice,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            });
        }

        public async Task<OrderFullDetailDTO?> GetOrderDetailsAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
            if (order == null) return null;

            return new OrderFullDetailDTO
            {
                Id = order.Id,
                UserId = order.UserId!,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                ShippingName = order.ShippingName,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderDetails.Select(d => new OrderItemDetailDTO
                {
                    BookId = d.BookId,
                    BookTitle = d.Book?.Title ?? "Sách đã bị xóa",
                    ImageUrl = d.Book?.ImageUrl ?? "",
                    Price = d.Price, 
                    Quantity = d.Quantity
                }).ToList()
            };
        }
    }
}
