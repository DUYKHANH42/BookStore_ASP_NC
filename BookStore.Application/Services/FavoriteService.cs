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
    public class FavoriteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFavoriteRepository _favoriteRepo;

        public FavoriteService(IUnitOfWork unitOfWork, IFavoriteRepository favoriteRepo)
        {
            _unitOfWork = unitOfWork;
            _favoriteRepo = favoriteRepo;
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int bookId)
        {
            var favorite = await _favoriteRepo.GetFavoriteAsync(userId, bookId);

            if (favorite != null)
            {
                _ = _favoriteRepo.DeleteAsync(favorite.Id); // Đã thích rồi thì bỏ thích
                await _unitOfWork.SaveChangesAsync();
                return false; // Trả về false nghĩa là đã gỡ khỏi danh sách
            }

            await _favoriteRepo.AddAsync(new Favorite { UserId = userId, BookId = bookId });
            await _unitOfWork.SaveChangesAsync();
            return true; // Trả về true nghĩa là đã thêm vào danh sách
        }

        public async Task<IEnumerable<FavoriteDto>> GetUserFavorites(string userId)
        {
            var list = await _favoriteRepo.GetUserFavoritesAsync(userId);
            return list.Select(f => new FavoriteDto
            {
                BookId = f.BookId,
                BookTitle = f.Book.Title,
                Price = f.Book.Price,
                ImageUrl = f.Book.ImageUrl
            });
        }
    }
}
