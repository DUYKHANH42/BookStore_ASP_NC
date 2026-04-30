using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<bool> ToggleFavoriteAsync(string userId, int productId)
        {
            var favorite = await _favoriteRepo.GetFavoriteAsync(userId, productId);

            if (favorite != null)
            {
                await _favoriteRepo.DeleteAsync(favorite.Id); 
                await _unitOfWork.SaveChangesAsync();
                return false; 
            }

            await _favoriteRepo.AddAsync(new Favorite { UserId = userId, ProductId = productId });
            await _unitOfWork.SaveChangesAsync();
            return true; 
        }

        public async Task<IEnumerable<FavoriteDto>> GetUserFavorites(string userId)
        {
            var list = await _favoriteRepo.GetUserFavoritesAsync(userId);
            return list.Select(f => new FavoriteDto
            {
                ProductId = f.ProductId,
                ProductName = f.Product.Name,
                Price = f.Product.Price,
                ImageUrl = f.Product.ImageUrl
            });
        }
    }
}
