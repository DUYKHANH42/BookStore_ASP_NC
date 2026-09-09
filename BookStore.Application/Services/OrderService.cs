using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services.Payment;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class OrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PaymentGatewayFactory _paymentGatewayFactory;
        private readonly IZaloPayService _zaloPayService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderService(
            IUnitOfWork unitOfWork,
            PaymentGatewayFactory paymentGatewayFactory,
            IZaloPayService zaloPayService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _paymentGatewayFactory = paymentGatewayFactory;
            _zaloPayService = zaloPayService;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<OrderDTO?> PlaceOrderAsync(string userId, CheckoutDTO checkoutDto, string operatorName = "System")
        {
            var cart = await _unitOfWork.Carts.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any()) return null;

            var order = new Order
            {
                OrderNumber = $"ORD-{TimeHelper.GetVnTime():yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                UserId = userId,
                ShippingName = checkoutDto.ShippingName,
                ShippingPhone = checkoutDto.ShippingPhone,
                ShippingAddress = checkoutDto.ShippingAddress,
                PaymentMethod = checkoutDto.PaymentMethod,
                Status = OrderStatus.Pending,
                CreatedAt = TimeHelper.GetVnTime()
            };

            decimal total = 0;
            foreach (var item in cart.Items)
            {
                var product = item.Product;
                if (product.Quantity < item.Quantity)
                    throw new InsufficientStockException(product.Name, item.Quantity, product.Quantity);

                int remainingToBuy = item.Quantity;
                var activeSale = await _unitOfWork.FlashSales.GetActiveSaleByProductIdAsync(product.Id);

                if (activeSale != null)
                {
                    int availableSaleSlots = activeSale.RemainingSlots; 

                    if (availableSaleSlots > 0)
                    {
                        int saleQty = Math.Min(remainingToBuy, availableSaleSlots);
                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = saleQty,
                            Price = activeSale.SalePrice,
                            FlashSaleId = activeSale.Id
                        });
                        total += activeSale.SalePrice * saleQty;
                        activeSale.SoldCount += saleQty;
                        remainingToBuy -= saleQty;
                    }
                }

                if (remainingToBuy > 0)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = remainingToBuy,
                        Price = product.Price
                    });
                    total += product.Price * remainingToBuy;
                }

                product.Quantity -= item.Quantity;
                var user = await _userManager.FindByIdAsync(userId);
                await _unitOfWork.StockHistories.AddAsync(new StockHistory
                {
                    ProductId = product.Id,
                    ChangeQuantity = -item.Quantity,
                    Reason = $"Bán hàng (Đơn hàng {order.OrderNumber})",
                    CreatedAt = TimeHelper.GetVnTime(),
                    ChangedBy = user?.FullName ?? operatorName,
                });
            }

            order.TotalPrice = total;
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.Carts.ClearCartAsync(userId);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException();
            }

            return new OrderDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt
            };
        }

        public async Task<CheckoutResultDTO> ProcessCheckoutAsync(string userId, CheckoutDTO checkoutDto, string userName, Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var orderDto = await PlaceOrderAsync(userId, checkoutDto, userName);
                if (orderDto == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return new CheckoutResultDTO { Success = false, Message = "Giỏ hàng trống." };
                }

                string? paymentUrl = null;

                if (checkoutDto.PaymentMethod != PaymentMethod.COD)
                {
                    var gateway = _paymentGatewayFactory.GetGateway(checkoutDto.PaymentMethod);
                    if (gateway == null)
                    {
                        await _unitOfWork.RollbackAsync();
                        return new CheckoutResultDTO { Success = false, Message = $"Phương thức {checkoutDto.PaymentMethod} không được hỗ trợ." };
                    }

                    try
                    {
                        paymentUrl = await gateway.CreatePaymentAsync(orderDto.Id, orderDto.TotalPrice, orderDto.OrderNumber, httpContext);
                    }
                    catch (NotSupportedException ex)
                    {
                        await _unitOfWork.RollbackAsync();
                        return new CheckoutResultDTO { Success = false, Message = ex.Message };
                    }

                    if (string.IsNullOrEmpty(paymentUrl))
                    {
                        await _unitOfWork.RollbackAsync();
                        return new CheckoutResultDTO { Success = false, Message = $"Không thể khởi tạo giao dịch {checkoutDto.PaymentMethod}." };
                    }
                }

                await _unitOfWork.CommitAsync();

                // Thông báo qua SignalR
                await _notificationService.SendAdminNotificationAsync(
                    "Đơn hàng mới",
                    $"Đơn hàng mới {orderDto.OrderNumber} đang chờ khách quét mã thanh toán ({checkoutDto.PaymentMethod}).", 
                    $"/Admin/Order?orderId={orderDto.Id}");

                return new CheckoutResultDTO
                {
                    Success = true,
                    Order = orderDto,
                    PaymentUrl = paymentUrl
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string Message)> ProcessZaloPayCallbackAsync(string dataStr, string mac)
        {
            if (!_zaloPayService.ValidateCallback(dataStr, mac))
            {
                return (false, "mac not equal");
            }

            var dataJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(dataStr);
            string app_trans_id = dataJson.app_trans_id;
            
            var parts = app_trans_id.Split('_');
            if (parts.Length <= 1) return (false, "Invalid trans id");

            string orderNumber = parts[1];
            var order = (await _unitOfWork.Orders.GetAllAsync()).FirstOrDefault(o => o.OrderNumber == orderNumber);
            
            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Paid;
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                await _notificationService.SendAdminNotificationAsync(
                    "Thanh toán thành công",
                    $"Đơn hàng {order.OrderNumber} đã thanh toán thành công qua ZaloPay.", 
                    $"/Admin/Order?orderId={order.Id}");
            }

            return (true, "success");
        }

        private async Task RestoreStockForCancelledOrder(Order order, string operatorName)
        {
            foreach (var detail in order.OrderDetails)
            {
                var product = detail.Product ?? await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                if (product == null) continue;

                product.Quantity += detail.Quantity;

                if (detail.FlashSaleId.HasValue)
                {
                    var flashSale = await _unitOfWork.FlashSales.GetByIdAsync(detail.FlashSaleId.Value);
                    if (flashSale != null)
                        flashSale.SoldCount = Math.Max(0, flashSale.SoldCount - detail.Quantity);
                }

                await _unitOfWork.StockHistories.AddAsync(new StockHistory
                {
                    ProductId = product.Id,
                    ChangeQuantity = detail.Quantity,
                    Reason = $"Hoàn kho & Sale (Hủy đơn hàng {order.OrderNumber})",
                    CreatedAt = TimeHelper.GetVnTime(),
                    ChangedBy = operatorName
                });
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, string operatorName = StockConstants.AdminOperator)
        {
            if (Enum.TryParse<OrderStatus>(status, true, out var newStatus))
            {
                var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
                if (order == null) return false;

                if (order.Status == OrderStatus.Cancelled) return false;

                if (newStatus == OrderStatus.Cancelled)
                {
                    await RestoreStockForCancelledOrder(order, operatorName);
                }

                order.Status = newStatus;
                await _unitOfWork.Orders.UpdateAsync(order);
                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            return false;
        }

        public async Task<PagedResultDTO<OrderDTO>> GetPagedOrdersAsync(int page, int pageSize, string status = "", string search = "")
        {
            OrderStatus? orderStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
                orderStatus = parsed;
            var (items, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(
                page, pageSize, orderStatus, string.IsNullOrEmpty(search) ? null : search);
            var orders = items.Select(o => new OrderDTO
            {
                Id = o.Id, OrderNumber = o.OrderNumber,
                TotalPrice = o.TotalPrice, Status = o.Status.ToString(), CreatedAt = o.CreatedAt
            }).ToList();
            return new PagedResultDTO<OrderDTO>
            {
                Items = orders, TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page, PageSize = pageSize
            };
        }

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

        public async Task<PagedResultDTO<OrderDTO>> GetUserOrdersPagedAsync(string userId, int page = 1, int pageSize = 5)
        {
            var (items, totalCount) = await _unitOfWork.Orders.GetUserOrdersPagedAsync(userId, page, pageSize);
            var orders = items.Select(o => new OrderDTO
            {
                Id = o.Id, OrderNumber = o.OrderNumber,
                TotalPrice = o.TotalPrice, Status = o.Status.ToString(), CreatedAt = o.CreatedAt
            }).ToList();
            return new PagedResultDTO<OrderDTO>
            {
                Items = orders, TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page, PageSize = pageSize
            };
        }

        public async Task<(bool Success, string Message)> CancelOrderForUserAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
            if (order == null) return (false, "Không tìm thấy đơn hàng.");
            
            if (order.UserId != userId) return (false, "Bạn không có quyền hủy đơn hàng này.");

            if (order.Status != OrderStatus.Pending)
            {
                return (false, "Chỉ có thể hủy đơn hàng khi đang ở trạng thái chờ xác nhận.");
            }

            var result = await UpdateOrderStatusAsync(orderId, "Cancelled", "Customer: " + userId);
            
            if (result) return (true, "Đã hủy đơn hàng thành công.");
            return (false, "Có lỗi xảy ra khi hủy đơn hàng.");
        }

        public async Task<IEnumerable<OrderFullDetailDTO>> GetAllOrdersForReportAsync(string status = "", string search = "")
        {
            OrderStatus? orderStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
                orderStatus = parsed;
            var orders = await _unitOfWork.Orders.GetOrdersForReportAsync(
                orderStatus, string.IsNullOrEmpty(search) ? null : search);
            return orders.Select(o => new OrderFullDetailDTO
            {
                Id = o.Id, OrderNumber = o.OrderNumber,
                UserId = o.UserId ?? "Guest", CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(), TotalPrice = o.TotalPrice,
                PaymentMethod = o.PaymentMethod.ToString(),
                ShippingName = o.ShippingName, ShippingPhone = o.ShippingPhone,
                ShippingAddress = o.ShippingAddress,
                Items = o.OrderDetails.Select(od => new OrderItemDetailDTO
                {
                    ProductId = od.ProductId, ProductName = od.Product?.Name ?? "N/A",
                    Price = od.Price, Quantity = od.Quantity
                }).ToList()
            }).ToList();
        }

        public async Task<bool> CancelExpiredOrderAsync(int orderId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
                if (order == null || order.Status != OrderStatus.Pending) return false;

                order.Status = OrderStatus.Cancelled;
                await RestoreStockForCancelledOrder(order, StockConstants.SystemCleanup);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                return false;
            }
        }

        public async Task<(bool Success, string Message, int OrderId, string OrderNumber, string ActivityDetails)> CreatePOSOrderAsync(POSOrderDTO request, string operatorName)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (request.Items == null || !request.Items.Any())
                    return (false, "Giỏ hàng trống", 0, string.Empty, string.Empty);

                var order = new Order
                {
                    OrderNumber = $"POS-{TimeHelper.GetVnTime():yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    UserId = null,
                    ShippingName = string.IsNullOrEmpty(request.CustomerName) ? "Khách lẻ" : request.CustomerName,
                    ShippingPhone = request.CustomerPhone,
                    ShippingAddress = "Mua tại cửa hàng",
                    PaymentMethod = request.PaymentMethod,
                    Status = OrderStatus.Completed,
                    CreatedAt = TimeHelper.GetVnTime(),
                    TotalPrice = request.TotalPrice
                };

                var soldSummaries = new List<string>();
                foreach (var item in request.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product == null) throw new NotFoundException("Product", item.ProductId);

                    if (product.Quantity < item.Quantity)
                        throw new InsufficientStockException(product.Name, item.Quantity, product.Quantity);

                    var detail = new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    order.OrderDetails.Add(detail);
                    soldSummaries.Add($"{product.Name} x{item.Quantity} ({item.Price:N0}đ)");

                    var activeSale = await _unitOfWork.FlashSales.GetActiveSaleByProductIdAsync(item.ProductId);
                    if (activeSale != null && item.Price == activeSale.SalePrice)
                    {
                        if (activeSale.RemainingSlots < item.Quantity)
                            throw new BusinessException($"Sản phẩm '{product.Name}' đã hết suất Flash Sale.");

                        activeSale.SoldCount += item.Quantity;
                    }

                    product.Quantity -= item.Quantity;

                    var stockLog = new StockHistory
                    {
                        ProductId = product.Id,
                        ChangeQuantity = -item.Quantity,
                        Reason = $"Bán tại POS (Đơn hàng {order.OrderNumber})",
                        CreatedAt = TimeHelper.GetVnTime(),
                        ChangedBy = operatorName
                    };
                    await _unitOfWork.StockHistories.AddAsync(stockLog);
                }

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                var paymentLabel = request.PaymentMethod == PaymentMethod.BankTransfer ? "Chuyển khoản" : "Tiền mặt";
                var customerLabel = $"{order.ShippingName} - {order.ShippingPhone}".Trim(' ', '-');
                var activityDetails = $"Tạo đơn POS {order.OrderNumber}: {string.Join("; ", soldSummaries)}. Khách: {customerLabel}. Thanh toán: {paymentLabel}. Tổng: {order.TotalPrice:N0}đ.";

                return (true, "Tạo đơn hàng POS thành công", order.Id, order.OrderNumber, activityDetails);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return (false, ex.Message, 0, string.Empty, string.Empty);
            }
        }
    }
}
