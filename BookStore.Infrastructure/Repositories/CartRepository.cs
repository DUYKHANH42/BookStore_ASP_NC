using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(BookStoreDbContext context) : base(context) { }


        public async Task<Cart> GetCartByUserIdAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Book)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart> AddToCartAsync(string userId, int bookId, int quantity)
        {
            var cart = await GetCartByUserIdAsync(userId);

            // Nếu chưa có giỏ thì tạo mới
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _context.Carts.AddAsync(cart);
            }

            var existingItem = cart.Items.FirstOrDefault(x => x.BookId == bookId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem { BookId = bookId, Quantity = quantity });
            }

            return cart;
        }
        public async Task<Cart> UpdateQuantityAsync(string userId, int bookId, int quantity)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart == null) return null!;

            var item = cart.Items.FirstOrDefault(x => x.BookId == bookId);
            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Items.Remove(item);
                }
            }

            return cart;
        }

        public async Task<Cart> RemoveFromCartAsync(string userId, int bookId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart == null) return null!;

            var item = cart.Items.FirstOrDefault(x => x.BookId == bookId);
            if (item != null)
            {
                cart.Items.Remove(item);
            }

            return cart;
        }
        public async Task<Cart> ClearCartAsync(string userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart != null)
            {
                cart.Items.Clear();
            }

            return cart ?? new Cart();
        }


    }
}