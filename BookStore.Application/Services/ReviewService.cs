using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class ReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // NGƯỜI DÙNG: Thêm hoặc cập nhật đánh giá
        public async Task<(bool success, string message)> SubmitReviewAsync(string userId, CreateReviewDTO dto)
        {
            // 1. Kiểm tra xem người dùng đã mua sản phẩm này chưa
            var hasPurchased = await _unitOfWork.Orders.HasPurchasedProductAsync(userId, dto.ProductId);
            if (!hasPurchased)
            {
                return (false, "Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua hàng.");
            }

            // 2. Kiểm tra xem đã có đánh giá chưa
            var existingReview = await _unitOfWork.Reviews.GetReviewByUserAndProductAsync(userId, dto.ProductId);

            if (existingReview != null)
            {
                // Cập nhật đánh giá cũ
                existingReview.Rating = dto.Rating;
                existingReview.Comment = dto.Comment;
                existingReview.UpdatedAt = DateTime.Now;
                await _unitOfWork.Reviews.UpdateAsync(existingReview);
                await _unitOfWork.SaveChangesAsync();
                return (true, "Đánh giá của bạn đã được cập nhật.");
            }
            else
            {
                // Thêm mới đánh giá
                var review = new Review
                {
                    ProductId = dto.ProductId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();
                return (true, "Cảm ơn bạn đã đánh giá sản phẩm.");
            }
        }

        // ADMIN: Lấy toàn bộ đánh giá (có phân trang)
        public async Task<IEnumerable<ReviewDTO>> GetAllReviewsAsync()
        {
            var reviews = await _unitOfWork.Reviews.GetAllAsync();
            return reviews.Select(r => new ReviewDTO
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product?.Name ?? "N/A",
                UserId = r.UserId,
                UserName = r.User?.UserName ?? "N/A",
                Rating = r.Rating,
                Comment = r.Comment,
                AdminReply = r.AdminReply,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                IsActive = r.IsActive
            }).OrderByDescending(x => x.CreatedAt);
        }

        // ADMIN: Phản hồi đánh giá
        public async Task<bool> ReplyToReviewAsync(AdminReplyDTO dto)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(dto.ReviewId);
            if (review == null) return false;

            review.AdminReply = dto.Reply;
            review.UpdatedAt = DateTime.Now;
            await _unitOfWork.Reviews.UpdateAsync(review);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        // ADMIN: Ẩn/Hiện đánh giá
        public async Task<bool> ToggleReviewStatusAsync(int reviewId)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null) return false;

            review.IsActive = !review.IsActive;
            await _unitOfWork.Reviews.UpdateAsync(review);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        // PUBLIC: Lấy đánh giá của một sản phẩm
        public async Task<IEnumerable<ReviewDTO>> GetProductReviewsAsync(int productId)
        {
            var reviews = await _unitOfWork.Reviews.GetReviewsByProductIdAsync(productId);
            return reviews.Where(r => r.IsActive).Select(r => new ReviewDTO
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product?.Name ?? "N/A",
                UserId = r.UserId,
                UserName = r.User?.UserName ?? "N/A",
                Rating = r.Rating,
                Comment = r.Comment,
                AdminReply = r.AdminReply,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task<bool> CanUserReviewProductAsync(string userId, int productId)
        {
            return await _unitOfWork.Orders.HasPurchasedProductAsync(userId, productId);
        }
    }
}
