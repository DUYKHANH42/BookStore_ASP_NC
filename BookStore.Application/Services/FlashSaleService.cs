using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class FlashSaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FlashSaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<FlashSaleManagementDTO>> GetSalesByProductIdAsync(int productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null) return Enumerable.Empty<FlashSaleManagementDTO>();

            var sales = await _unitOfWork.FlashSales.GetSalesByProductIdAsync(productId);
            var now = DateTime.Now;

            return sales.Select(s => new FlashSaleManagementDTO
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = product.Name,
                OriginalPrice = product.Price,
                SalePrice = s.SalePrice,
                SaleStock = s.SaleStock,
                SoldCount = s.SoldCount,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsActive = s.IsActive,
                Status = GetSaleStatus(s, now)
            });
        }

        public async Task<bool> CreateFlashSaleAsync(FlashSaleCreateDTO dto)
        {
            // 1. Kiểm tra sản phẩm tồn tại
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null) throw new Exception("Sản phẩm không tồn tại");

            // 2. Validate thời gian
            if (dto.StartTime >= dto.EndTime) throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc");
            if (dto.EndTime <= DateTime.Now) throw new Exception("Thời gian kết thúc phải ở tương lai");

            // 3. Kiểm tra trùng lặp thời gian cho cùng 1 sản phẩm
            var existingSales = await _unitOfWork.FlashSales.GetSalesByProductIdAsync(dto.ProductId);
            bool isOverlapping = existingSales.Any(s => 
                s.IsActive && 
                ((dto.StartTime >= s.StartTime && dto.StartTime <= s.EndTime) || 
                 (dto.EndTime >= s.StartTime && dto.EndTime <= s.EndTime) ||
                 (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)));

            if (isOverlapping) throw new Exception("Sản phẩm đã có một chương trình Flash Sale khác trong khoảng thời gian này");

            // 4. Tạo mới
            var flashSale = new FlashSale
            {
                ProductId = dto.ProductId,
                SalePrice = dto.SalePrice,
                SaleStock = dto.SaleStock,
                SoldCount = 0,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsActive = true
            };

            await _unitOfWork.FlashSales.AddAsync(flashSale);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleSaleStatusAsync(int saleId)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(saleId);
            if (sale == null) return false;

            sale.IsActive = !sale.IsActive;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteSaleAsync(int saleId)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(saleId);
            if (sale == null) return false;
            
            if (sale.SoldCount > 0) throw new Exception("Không thể xóa chương trình sale đã có lượt mua. Hãy chọn Tắt thay vì Xóa.");
            await _unitOfWork.FlashSales.DeleteAsync(sale.Id);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        private string GetSaleStatus(FlashSale s, DateTime now) 
        {
            if (!s.IsActive) return "Đã tắt";
            if (s.SoldCount >= s.SaleStock) return "Hết suất";
            if (now < s.StartTime) return "Sắp diễn ra";
            if (now > s.EndTime) return "Đã kết thúc";
            return "Đang diễn ra";
        }
    }
}
