using BookStore.Application.DTO;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookStore.Application.Interfaces;

namespace BookStore.Application.Services
{
    public class ProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public ProductService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<PagedResultDTO<ProductDTO>> GetProductsAsync(ProductQueryParameters parameters)
        {
            var (items, totalCount) = await _unitOfWork.Products.GetFilteredPagedAsync(parameters);

            var dtos = items.Select(b => MapToDTO(b));

            return new PagedResultDTO<ProductDTO>
            {
                Items = dtos,
                TotalItems = totalCount,
                CurrentPage = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
            };
        }

        public async Task<IEnumerable<ProductDTO>> GetAllProductsAsync()
        {
            var (items, _) = await _unitOfWork.Products.GetFilteredPagedAsync(new ProductQueryParameters { PageSize = 1000 });
            return items.Select(b => MapToDTO(b));
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
            return product != null ? MapToDTO(product) : null;
        }

        public async Task<IEnumerable<ProductDTO>> GetNewArrivalsAsync(int count)
        {
            var products = await _unitOfWork.Products.GetNewArrivalsAsync(count);
            return products.Select(b => MapToDTO(b));
        }

        public async Task<IEnumerable<ProductDTO>> GetRelatedProductsAsync(int productId, int count)
        {
            var products = await _unitOfWork.Products.GetRelatedProductsAsync(productId, count);
            return products.Select(b => MapToDTO(b));
        }

        public async Task<IEnumerable<ProductDTO>> GetFlashSaleProductsAsync(int count)
        {
            var (items, _) = await _unitOfWork.Products.GetFilteredPagedAsync(new ProductQueryParameters 
            { 
                IsFlashSale = true, 
                PageSize = count 
            });
            return items.Select(b => MapToDTO(b));
        }

        public async Task<bool> CreateProductAsync(ProductCreateDTO dto, string imageUrl, string userName, List<string>? additionalImages = null)
        {
            var product = new Product
            {
                Name = dto.Name,
                Brand = dto.Brand,
                Description = dto.Description,
                Price = dto.Price,
                Quantity = dto.Quantity,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                SKU = dto.SKU,
                ImageUrl = imageUrl,
                IsActive = true,
                CreatedAt = BookStore.Domain.Common.TimeHelper.GetVnTime(),
                CreatedBy = userName
            };

            // Add main image to ProductImage table
            product.Images.Add(new ProductImage
            {
                ImageUrl = imageUrl,
                IsMain = true,
                DisplayOrder = 0
            });

            // Add additional images
            if (additionalImages != null && additionalImages.Any())
            {
                int order = 1;
                foreach (var img in additionalImages)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = img,
                        IsMain = false,
                        DisplayOrder = order++
                    });
                }
            }

            await _unitOfWork.Products.AddAsync(product);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateProductAsync(int id, ProductCreateDTO dto, string? imageUrl, string userName, List<string>? additionalImages = null)
        {
            var product = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
            if (product == null) return false;

            product.Name = dto.Name;
            product.Brand = dto.Brand;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Quantity = dto.Quantity;
            product.CategoryId = dto.CategoryId;
            product.SubCategoryId = dto.SubCategoryId;
            product.SKU = dto.SKU;
            product.UpdatedAt = BookStore.Domain.Common.TimeHelper.GetVnTime();
            product.UpdatedBy = userName;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                // BƯỚC QUAN TRỌNG: XÓA FILE CŨ TRÊN CLOUD
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "default_product.png")
                {
                    await _fileService.DeleteFileAsync(product.ImageUrl);
                }

                product.ImageUrl = imageUrl;
                
                // Cập nhật / Thay thế ảnh Main trong bảng ProductImages
                var existingMain = product.Images.FirstOrDefault(i => i.IsMain);
                if (existingMain != null)
                {
                    existingMain.ImageUrl = imageUrl;
                }
                else
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = imageUrl,
                        IsMain = true,
                        DisplayOrder = 0
                    });
                }
            }

            if (additionalImages != null && additionalImages.Any())
            {
                // 1. XÓA BỘ SƯU TẬP CŨ KHỎI CẢ CLOUDINARY VÀ CSDL ĐỂ THAY THẾ
                var oldAdditional = product.Images.Where(i => !i.IsMain).ToList();
                foreach (var oldImg in oldAdditional)
                {
                    if (!string.IsNullOrEmpty(oldImg.ImageUrl) && oldImg.ImageUrl.StartsWith("http"))
                    {
                        await _fileService.DeleteFileAsync(oldImg.ImageUrl);
                    }
                    product.Images.Remove(oldImg);
                }

                // 2. THÊM BỘ SƯU TẬP MỚI
                int order = 1;
                foreach (var img in additionalImages)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = img,
                        IsMain = false,
                        DisplayOrder = order++
                    });
                }
            }

            await _unitOfWork.Products.UpdateAsync(product);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await _unitOfWork.ProductImages.GetByIdAsync(imageId);
            if (image == null) return false;

            // XÓA FILE VẬT LÝ TRÊN CLOUD
            if (!string.IsNullOrEmpty(image.ImageUrl) && image.ImageUrl.StartsWith("http"))
            {
                await _fileService.DeleteFileAsync(image.ImageUrl);
            }

            await _unitOfWork.ProductImages.DeleteAsync(imageId);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        private ProductDTO MapToDTO(Product product)
        {
            var dto = new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "",
                SubCategoryId = product.SubCategoryId,
                SubCategoryName = product.SubCategory?.Name ?? "",
                SKU = product.SKU,
                IsActive = product.IsActive,
                Images = product.Images?.Select(i => new ProductImageDTO
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    DisplayOrder = i.DisplayOrder
                }).ToList() ?? new List<ProductImageDTO>()
            };

            // TÌM FLASH SALE ĐANG HIỆU LỰC
            var now = BookStore.Domain.Common.TimeHelper.GetVnTime();
            var activeSale = product.FlashSales?.FirstOrDefault(s => 
                s.IsActive && s.StartTime <= now && s.EndTime >= now && s.SoldCount < s.SaleStock);

            if (activeSale != null)
            {
                dto.FlashSale = new FlashSaleDTO
                {
                    Id = activeSale.Id,
                    SalePrice = activeSale.SalePrice,
                    SaleStock = activeSale.SaleStock,
                    SoldCount = activeSale.SoldCount,
                    StartTime = activeSale.StartTime,
                    EndTime = activeSale.EndTime,
                    RemainingSlots = activeSale.SaleStock - activeSale.SoldCount
                };
            }

            return dto;
        }
    }
}
