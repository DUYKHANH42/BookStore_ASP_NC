using BookStore.Application.DTO;
using BookStore.Domain.Common;
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
            var now = BookStore.Domain.Common.TimeHelper.GetVnTime();

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
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId) 
                ?? throw new Exception("Sản phẩm không tồn tại");

            ValidateTime(dto);
            await CheckOverlappingAsync(dto.ProductId, dto);

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

        public async Task<bool> UpdateFlashSaleAsync(FlashSaleUpdateDTO dto)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(dto.Id) 
                ?? throw new Exception("Flash Sale không tồn tại");

            ValidateTime(dto);
            if (dto.SaleStock < sale.SoldCount) throw new Exception("Số lượng sale không thể ít hơn số đã bán");
            await CheckOverlappingAsync(sale.ProductId, dto, sale.Id);

            sale.SalePrice = dto.SalePrice;
            sale.SaleStock = dto.SaleStock;
            sale.StartTime = dto.StartTime;
            sale.EndTime = dto.EndTime;

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteSaleAsync(int saleId)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(saleId) 
                ?? throw new Exception("Flash Sale không tồn tại");
            
            if (sale.SoldCount > 0) throw new Exception("Không thể xóa chương trình sale đã có lượt mua. Hãy chọn Tắt thay vì Xóa.");
            
            await _unitOfWork.FlashSales.DeleteAsync(sale.Id);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<FlashSaleManagementDTO?> GetSaleByIdAsync(int saleId)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(saleId);
            if (sale == null) return null;

            var product = await _unitOfWork.Products.GetByIdAsync(sale.ProductId);
            var now = TimeHelper.GetVnTime();

            return new FlashSaleManagementDTO
            {
                Id = sale.Id,
                ProductId = sale.ProductId,
                ProductName = product?.Name ?? "N/A",
                OriginalPrice = product?.Price ?? 0,
                SalePrice = sale.SalePrice,
                SaleStock = sale.SaleStock,
                SoldCount = sale.SoldCount,
                StartTime = sale.StartTime,
                EndTime = sale.EndTime,
                IsActive = sale.IsActive,
                Status = GetSaleStatus(sale, now)
            };
        }

        public async Task<bool> ToggleSaleStatusAsync(int saleId)
        {
            var sale = await _unitOfWork.FlashSales.GetByIdAsync(saleId) 
                ?? throw new Exception("Flash Sale không tồn tại");

            sale.IsActive = !sale.IsActive;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        private void ValidateTime(FlashSaleBaseDTO dto)
        {
            if (dto.StartTime >= dto.EndTime) throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc");
            if (dto.EndTime <= TimeHelper.GetVnTime()) throw new Exception("Thời gian kết thúc phải ở tương lai");
        }

        private async Task CheckOverlappingAsync(int productId, FlashSaleBaseDTO dto, int? excludeSaleId = null)
        {
            var existingSales = await _unitOfWork.FlashSales.GetSalesByProductIdAsync(productId);
            bool isOverlapping = existingSales.Any(s =>
                s.Id != excludeSaleId && s.IsActive &&
                ((dto.StartTime >= s.StartTime && dto.StartTime <= s.EndTime) ||
                 (dto.EndTime >= s.StartTime && dto.EndTime <= s.EndTime) ||
                 (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)));

            if (isOverlapping) throw new Exception("Sản phẩm đã có một chương trình Flash Sale khác trong khoảng thời gian này");
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

