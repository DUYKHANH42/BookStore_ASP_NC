using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<CartDTO> AddToCartAsync(string userId, int bookId, int quantity)
        {
            var cart = await _unitOfWork.Carts.AddToCartAsync(userId, bookId, quantity);
            await _unitOfWork.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDTO> UpdateQuantityAsync(string userId, int bookId, int quantity)
        {
            var cart = await _unitOfWork.Carts.UpdateQuantityAsync(userId, bookId, quantity);
            if (cart != null)
            {
                await _unitOfWork.SaveChangesAsync();
            }
            return MapToDto(cart!);
        }

        public async Task<CartDTO> RemoveFromCartAsync(string userId, int bookId)
        {
            var cart = await _unitOfWork.Carts.RemoveFromCartAsync(userId, bookId);
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
                    decimal effectivePrice = (i.Book.IsFlashSale && i.Book.DiscountPrice.HasValue)
                             ? i.Book.DiscountPrice.Value
                             : i.Book.Price;

                    return new CartItemDTO
                    {
                        Id = i.Id,
                        BookId = i.BookId,
                        BookTitle = i.Book?.Title ?? "Unknown",
                        ImageUrl = i.Book?.ImageUrl ?? "",
                        Author = i.Book?.Author ?? "",

                        OriginalPrice = i.Book?.Price ?? 0,
                        Price = effectivePrice,

                        Quantity = i.Quantity
                    };
                }).ToList()
            };
        }
    }
}
