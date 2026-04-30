using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class CartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDTO> GetCartAsync(string userId)
        {
            var cart = await _unitOfWork.Carts.GetCartByUserIdAsync(userId);
            return MapToDto(cart ?? new Cart { UserId = userId });
        }

        public async Task<CartDTO> AddToCartAsync(string userId, int productId, int quantity)
        {
            // Repository vẫn dùng tên cũ hoặc cần refactor Repository trước
            // Ở đây giả sử ICartRepository đã được refactor để dùng productId
            var cart = await _unitOfWork.Carts.AddToCartAsync(userId, productId, quantity);
            await _unitOfWork.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDTO> UpdateQuantityAsync(string userId, int productId, int quantity)
        {
            var cart = await _unitOfWork.Carts.UpdateQuantityAsync(userId, productId, quantity);
            if (cart != null)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return MapToDto(cart!);
        }

        public async Task<CartDTO> RemoveFromCartAsync(string userId, int productId)
        {
            var cart = await _unitOfWork.Carts.RemoveFromCartAsync(userId, productId);
            if (cart != null)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return MapToDto(cart!);
        }

        public async Task<CartDTO> ClearCartAsync(string userId)
        {
            var cart = await _unitOfWork.Carts.ClearCartAsync(userId);
            await _unitOfWork.SaveChangesAsync();
            return new CartDTO();
        }

        private CartDTO MapToDto(Cart cart)
        {
            return new CartDTO
            {
                Id = cart.Id,
                Items = cart.Items.Select(i =>
                {
                    // LOGIC TÍNH GIÁ CHUẨN ĐỒNG NHẤT VỚI ORDER SERVICE
                    decimal effectivePrice = i.Product.Price;
                    
                    var now = DateTime.Now;
                    var activeSale = i.Product.FlashSales?.FirstOrDefault(s => 
                        s.IsActive && s.StartTime <= now && s.EndTime >= now && s.SoldCount < s.SaleStock);

                    if (activeSale != null && i.Quantity <= (activeSale.SaleStock - activeSale.SoldCount))
                    {
                        effectivePrice = activeSale.SalePrice;
                    }

                    return new CartItemDTO
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? "Unknown",
                        ImageUrl = i.Product?.ImageUrl ?? "",
                        Brand = i.Product?.Brand ?? "",

                        OriginalPrice = i.Product?.Price ?? 0,
                        Price = effectivePrice,

                        Quantity = i.Quantity
                    };
                }).ToList()
            };
        }
    }
}
