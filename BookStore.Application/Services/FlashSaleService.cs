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

        public async Task<PagedResultDTO<FlashSaleCampaignDTO>> GetPagedCampaignsAsync(string search, string status, int page, int pageSize)
        {
            var allCampaigns = await _unitOfWork.FlashSaleCampaigns.GetAllAsync();
            var allSales = await _unitOfWork.FlashSales.GetAllAsync();
            var now = TimeHelper.GetVnTime();

            var query = allCampaigns.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            //var list = query.Select(c => {
            //    var campaignSales = allSales.Where(s => s.FlashSaleCampaignId == c.Id).ToList();
            //    return new FlashSaleCampaignDTO
            //    {
            //        Id = c.Id,
            //        Name = c.Name,
            //        StartTime = c.StartTime,
            //        EndTime = c.EndTime,
            //        IsActive = c.IsActive,
            //        Status = GetCampaignStatus(c, now),
            //        ProductCount = campaignSales.Count,
            //        TotalStock = campaignSales.Sum(s => s.SaleStock),
            //        TotalSold = campaignSales.Sum(s => s.SoldCount)
            //    };
            //}).ToList();
            var list = query.Select(c => new FlashSaleCampaignDTO
            {
                Id = c.Id,
                Name = c.Name,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                IsActive = c.IsActive,
                Status = GetCampaignStatus(c, now),
                ProductCount = allSales.Count(s => s.FlashSaleCampaignId == c.Id),
                TotalStock = allSales.Where(s => s.FlashSaleCampaignId == c.Id).Sum(s => s.SaleStock),
                TotalSold = allSales.Where(s => s.FlashSaleCampaignId == c.Id).Sum(s => s.SoldCount)
            }).ToList();

            if (!string.IsNullOrEmpty(status))
            {
                list = list.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            list = list.OrderByDescending(x => x.StartTime).ToList();

            int totalItems = list.Count;
            var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResultDTO<FlashSaleCampaignDTO>
            {
                Items = items,
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<int> CreateCampaignAsync(FlashSaleCampaignCreateDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("Danh sách sản phẩm không được rỗng");

            if (dto.StartTime >= dto.EndTime)
                throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc");

            if (dto.EndTime <= TimeHelper.GetVnTime())
                throw new Exception("Thời gian kết thúc phải ở tương lai");

            var campaign = new FlashSaleCampaign
            {
                Name = dto.Name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsActive = true
            };

            await _unitOfWork.FlashSaleCampaigns.AddAsync(campaign);
            await _unitOfWork.SaveChangesAsync(); // Save to get Campaign Id

            int addedCount = 0;
            foreach (var item in dto.Items)
            {
                var existingSales = await _unitOfWork.FlashSales.GetSalesByProductIdAsync(item.ProductId);
                bool isOverlapping = existingSales.Any(s =>
                    s.FlashSaleCampaign != null &&
                    s.FlashSaleCampaign.IsActive &&
                    ((dto.StartTime >= s.FlashSaleCampaign.StartTime && dto.StartTime <= s.FlashSaleCampaign.EndTime) ||
                     (dto.EndTime >= s.FlashSaleCampaign.StartTime && dto.EndTime <= s.FlashSaleCampaign.EndTime) ||
                     (dto.StartTime <= s.FlashSaleCampaign.StartTime && dto.EndTime >= s.FlashSaleCampaign.EndTime)));

                if (isOverlapping) continue;

                var flashSale = new FlashSale
                {
                    FlashSaleCampaignId = campaign.Id,
                    ProductId = item.ProductId,
                    SalePrice = item.SalePrice,
                    SaleStock = item.SaleStock,
                    SoldCount = 0
                };

                await _unitOfWork.FlashSales.AddAsync(flashSale);
                addedCount++;
            }

            if (addedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return addedCount;
        }

        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            var campaign = await _unitOfWork.FlashSaleCampaigns.GetCampaignWithSalesAsync(campaignId) 
                ?? throw new Exception("Chiến dịch Sale không tồn tại");
            
            if (campaign.FlashSales.Any(s => s.SoldCount > 0)) 
                throw new Exception("Không thể xóa chiến dịch đã có lượt mua. Hãy chọn Tắt thay vì Xóa.");
            
            foreach (var sale in campaign.FlashSales.ToList())
            {
                await _unitOfWork.FlashSales.DeleteAsync(sale.Id);
            }
            
            await _unitOfWork.FlashSaleCampaigns.DeleteAsync(campaign.Id);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleCampaignStatusAsync(int campaignId)
        {
            var campaign = await _unitOfWork.FlashSaleCampaigns.GetByIdAsync(campaignId) 
                ?? throw new Exception("Chiến dịch Sale không tồn tại");

            campaign.IsActive = !campaign.IsActive;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<FlashSaleManagementDTO>> GetCampaignItemsAsync(int campaignId)
        {
            var campaign = await _unitOfWork.FlashSaleCampaigns.GetCampaignWithSalesAsync(campaignId);
            if (campaign == null) return Enumerable.Empty<FlashSaleManagementDTO>();

            var now = TimeHelper.GetVnTime();

            return campaign.FlashSales.Select(s => new FlashSaleManagementDTO
            {
                Id = s.Id,
                FlashSaleCampaignId = campaign.Id,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                ImageUrl = s.Product.ImageUrl,
                OriginalPrice = s.Product.Price,
                SalePrice = s.SalePrice,
                SaleStock = s.SaleStock,
                SoldCount = s.SoldCount,
                StartTime = campaign.StartTime,
                EndTime = campaign.EndTime,
                IsActive = campaign.IsActive,
                Status = GetCampaignStatus(campaign, now)
            });
        }

        private string GetCampaignStatus(FlashSaleCampaign c, DateTime now) 
        {
            if (!c.IsActive) return "Đã tắt";
            if (now < c.StartTime) return "Sắp diễn ra";
            if (now > c.EndTime) return "Đã kết thúc";
            return "Đang diễn ra";
        }

        public async Task<FlashSaleCampaignEditDTO> GetCampaignForEditAsync(int id)
        {
            var campaign = await _unitOfWork.FlashSaleCampaigns.GetCampaignWithSalesAsync(id)
                ?? throw new Exception("Chiến dịch Flash Sale không tồn tại");

            return new FlashSaleCampaignEditDTO
            {
                Id = campaign.Id,
                Name = campaign.Name,
                StartTime = campaign.StartTime,
                EndTime = campaign.EndTime,
                IsActive = campaign.IsActive,
                Items = campaign.FlashSales.Select(s => new FlashSaleItemEditDTO
                {
                    Id = s.Id,
                    ProductId = s.ProductId,
                    ProductName = s.Product.Name,
                    ImageUrl = s.Product.ImageUrl,
                    OriginalPrice = s.Product.Price,
                    SalePrice = s.SalePrice,
                    SaleStock = s.SaleStock,
                    SoldCount = s.SoldCount
                }).ToList()
            };
        }

        public async Task<bool> UpdateCampaignAsync(FlashSaleCampaignEditDTO dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("Danh sách sản phẩm không được rỗng");

            if (dto.StartTime >= dto.EndTime)
                throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc");

            var campaign = await _unitOfWork.FlashSaleCampaigns.GetCampaignWithSalesAsync(dto.Id)
                ?? throw new Exception("Không tìm thấy chiến dịch Flash Sale");

            campaign.Name = dto.Name;
            campaign.StartTime = dto.StartTime;
            campaign.EndTime = dto.EndTime;
            campaign.IsActive = dto.IsActive;

            var dbSales = campaign.FlashSales.ToList();
            var incomingProductIds = dto.Items.Select(i => i.ProductId).ToList();

            // 1. Remove deleted items
            foreach (var sale in dbSales)
            {
                if (!incomingProductIds.Contains(sale.ProductId))
                {
                    if (sale.SoldCount > 0)
                        throw new Exception($"Không thể xóa sản phẩm {sale.Product.Name} khỏi chiến dịch vì đã có lượt mua.");
                    
                    await _unitOfWork.FlashSales.DeleteAsync(sale.Id);
                }
            }

            // 2. Add or Update items
            foreach (var item in dto.Items)
            {
                var existingSale = dbSales.FirstOrDefault(s => s.ProductId == item.ProductId);
                if (existingSale != null)
                {
                    if (item.SaleStock < existingSale.SoldCount)
                        throw new Exception($"Số lượng kho sale cho {existingSale.Product.Name} không thể nhỏ hơn số lượng đã bán ({existingSale.SoldCount}).");

                    existingSale.SalePrice = item.SalePrice;
                    existingSale.SaleStock = item.SaleStock;
                    await _unitOfWork.FlashSales.UpdateAsync(existingSale);
                }
                else
                {
                    // Newly added product - Check overlapping campaigns
                    var existingSales = await _unitOfWork.FlashSales.GetSalesByProductIdAsync(item.ProductId);
                    bool isOverlapping = existingSales.Any(s =>
                        s.FlashSaleCampaignId != campaign.Id &&
                        s.FlashSaleCampaign != null &&
                        s.FlashSaleCampaign.IsActive &&
                        ((dto.StartTime >= s.FlashSaleCampaign.StartTime && dto.StartTime <= s.FlashSaleCampaign.EndTime) ||
                         (dto.EndTime >= s.FlashSaleCampaign.StartTime && dto.EndTime <= s.FlashSaleCampaign.EndTime) ||
                         (dto.StartTime <= s.FlashSaleCampaign.StartTime && dto.EndTime >= s.FlashSaleCampaign.EndTime)));

                    if (isOverlapping)
                    {
                        var prod = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                        string prodName = prod?.Name ?? $"ID {item.ProductId}";
                        throw new Exception($"Sản phẩm '{prodName}' đã tham gia chiến dịch khác trong khoảng thời gian này.");
                    }

                    var newSale = new FlashSale
                    {
                        FlashSaleCampaignId = campaign.Id,
                        ProductId = item.ProductId,
                        SalePrice = item.SalePrice,
                        SaleStock = item.SaleStock,
                        SoldCount = 0
                    };
                    await _unitOfWork.FlashSales.AddAsync(newSale);
                }
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Product>> SearchProductsByNameAsync(string term)
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            
            if (string.IsNullOrEmpty(term))
            {
                return products.Take(10);
            }
            return products.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(20);
        }
    }
}

