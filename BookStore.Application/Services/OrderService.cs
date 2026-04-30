using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // HÀM CỦA BẠN: Đặt hàng, trừ tồn kho và ghi log
        public async Task<OrderDTO?> PlaceOrderAsync(string userId, CheckoutDTO checkoutDto, string operatorName = "System")
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
                var product = item.Product; 
                
                if (product.Quantity < item.Quantity)
                {
                    throw new Exception($"Sản phẩm {product.Name} không đủ số lượng trong kho.");
                }

                int remainingToBuy = item.Quantity;

                // 1. Kiểm tra Flash Sale đang hoạt động
                var activeSale = await _unitOfWork.FlashSales.GetActiveSaleByProductIdAsync(product.Id);
                
                if (activeSale != null)
                {
                    int availableSaleSlots = activeSale.SaleStock - activeSale.SoldCount;

                    if (availableSaleSlots > 0)
                    {
                        int saleQty = Math.Min(remainingToBuy, availableSaleSlots);
                        
                        // Dòng chi tiết cho phần GIÁ SALE
                        var saleDetail = new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = saleQty,
                            Price = activeSale.SalePrice,
                            FlashSaleId = activeSale.Id // GÁN ID ĐỂ THEO DÕI
                        };
                        order.OrderDetails.Add(saleDetail);
                        total += activeSale.SalePrice * saleQty;
                        
                        // Cập nhật SoldCount cho FlashSale
                        activeSale.SoldCount += saleQty;
                        if (activeSale.SoldCount >= activeSale.SaleStock)
                        {
                            activeSale.IsActive = false; // Tắt sale ngay lập tức
                        }

                        remainingToBuy -= saleQty;
                    }
                }

                // 2. Nếu vẫn còn số lượng (do không có sale hoặc sale hết suất giữa chừng)
                if (remainingToBuy > 0)
                {
                    var regularDetail = new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = remainingToBuy,
                        Price = product.Price 
                    };
                    order.OrderDetails.Add(regularDetail);
                    total += product.Price * remainingToBuy;
                }

                // CẬP NHẬT TỒN KHO TỔNG
                product.Quantity -= item.Quantity;

                var stockLog = new StockHistory
                {
                    ProductId = product.Id,
                    ChangeQuantity = -item.Quantity,
                    Reason = $"Bán hàng (Đơn hàng {order.OrderNumber})",
                    CreatedAt = DateTime.Now,
                    ChangedBy = operatorName
                };
                await _unitOfWork.StockHistories.AddAsync(stockLog);
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

        // HÀM CHO ADMIN: Cập nhật trạng thái đơn hàng và xử lý HOÀN KHO (TÍCH HỢP HOÀN SALE)
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, string operatorName = "Admin")
        {
            if (Enum.TryParse<OrderStatus>(status, true, out var newStatus))
            {
                var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
                if (order == null) return false;

                if (order.Status == OrderStatus.Cancelled) return false;

                // XỬ LÝ HOÀN KHO KHI HỦY ĐƠN
                if (newStatus == OrderStatus.Cancelled)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Quantity += detail.Quantity;
                            
                            // HOÀN LẠI SUẤT SALE CHÍNH XÁC
                            if (detail.FlashSaleId.HasValue)
                            {
                                var flashSale = await _unitOfWork.FlashSales.GetByIdAsync(detail.FlashSaleId.Value);
                                if (flashSale != null)
                                {
                                    flashSale.SoldCount = Math.Max(0, flashSale.SoldCount - detail.Quantity);
                                    // Nếu trước đó sale bị tắt do hết suất, giờ có suất lại thì bật lên (Tùy nghiệp vụ, ở đây t bật lại)
                                    if (flashSale.SoldCount < flashSale.SaleStock && flashSale.EndTime > DateTime.Now)
                                    {
                                        flashSale.IsActive = true;
                                    }
                                }
                            }

                            var stockLog = new StockHistory
                            {
                                ProductId = detail.ProductId,
                                ChangeQuantity = detail.Quantity,
                                Reason = $"Hoàn kho & Sale (Hủy đơn hàng {order.OrderNumber})",
                                CreatedAt = DateTime.Now,
                                ChangedBy = operatorName
                            };
                            await _unitOfWork.StockHistories.AddAsync(stockLog);
                        }
                    }
                }

                order.Status = newStatus;
                await _unitOfWork.Orders.UpdateAsync(order);
                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            return false;
        }

        // HÀM CHO ADMIN: Lấy danh sách đơn hàng có phân trang và lọc
        public async Task<PagedResultDTO<OrderDTO>> GetPagedOrdersAsync(int page, int pageSize, string status = "", string search = "")
        {
            var query = await _unitOfWork.Orders.GetAllAsync();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    query = query.Where(o => o.Status == orderStatus);
                }
            }

            // Lọc theo từ khóa tìm kiếm (Mã đơn hàng)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var totalItems = query.Count();
            
            var orders = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDTO
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt
                }).ToList();

            return new PagedResultDTO<OrderDTO>
            {
                Items = orders,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        // HÀM CHO ADMIN & PDF: Lấy chi tiết đơn hàng
        public async Task<OrderFullDetailDTO?> GetOrderDetailsAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
            if (order == null) return null;

            return new OrderFullDetailDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId ?? "Guest",
                CreatedAt = order.CreatedAt,
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                PaymentMethod = order.PaymentMethod.ToString(),
                ShippingName = order.ShippingName,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderDetails.Select(od => new OrderItemDetailDTO
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? "Sản phẩm không tồn tại",
                    ImageUrl = od.Product?.ImageUrl ?? "default_product.png",
                    Price = od.Price,
                    Quantity = od.Quantity
                }).ToList()
            };
        }

        // HÀM CỦA BẠN: Lấy lịch sử đơn hàng của User
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


        // HÀM CHO BÁO CÁO: Lấy toàn bộ đơn hàng kèm chi tiết
        public async Task<IEnumerable<OrderFullDetailDTO>> GetAllOrdersForReportAsync(string status = "", string search = "")
        {
            var query = await _unitOfWork.Orders.GetAllAsync();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
            var result = new List<OrderFullDetailDTO>();

            foreach (var order in orders)
            {
                // Lấy chi tiết cho từng đơn hàng (Để có đầy đủ thông tin sản phẩm)
                var detailedOrder = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(order.Id);
                if (detailedOrder != null)
                {
                    result.Add(new OrderFullDetailDTO
                    {
                        Id = detailedOrder.Id,
                        OrderNumber = detailedOrder.OrderNumber,
                        UserId = detailedOrder.UserId ?? "Guest",
                        CreatedAt = detailedOrder.CreatedAt,
                        Status = detailedOrder.Status.ToString(),
                        TotalPrice = detailedOrder.TotalPrice,
                        PaymentMethod = detailedOrder.PaymentMethod.ToString(),
                        ShippingName = detailedOrder.ShippingName,
                        ShippingPhone = detailedOrder.ShippingPhone,
                        ShippingAddress = detailedOrder.ShippingAddress,
                        Items = detailedOrder.OrderDetails.Select(od => new OrderItemDetailDTO
                        {
                            ProductId = od.ProductId,
                            ProductName = od.Product?.Name ?? "N/A",
                            Price = od.Price,
                            Quantity = od.Quantity
                        }).ToList()
                    });
                }
            }

            return result;
        }
    }
}
