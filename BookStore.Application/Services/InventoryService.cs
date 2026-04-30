using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class InventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> ImportStockAsync(StockImportDTO dto, string adminName, string? adminId, string? imageUrl, List<string>? additionalImages = null)
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var product = products.FirstOrDefault(p => p.SKU == dto.SKU);

            bool isNewProduct = false;

            if (product == null)
            {
                isNewProduct = true;
                if (string.IsNullOrEmpty(dto.Name)) throw new Exception("Tên sản phẩm không được để trống khi tạo mới");

                product = new Product
                {
                    SKU = dto.SKU,
                    Name = dto.Name,
                    Brand = dto.Brand ?? "Unknown",
                    Description = dto.Description ?? "",
                    Price = dto.SellingPrice ?? 0,
                    Quantity = dto.QuantityToImport,
                    CategoryId = dto.CategoryId ?? 1,
                    SubCategoryId = dto.SubCategoryId,
                    ImageUrl = imageUrl ?? "default_product.png",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    CreatedBy = adminName
                };

                // Thêm ảnh chính vào bảng ProductImage
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = imageUrl,
                        IsMain = true,
                        DisplayOrder = 0
                    });
                }

                // Thêm ảnh phụ
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
                // Cần SaveChanges để lấy được ProductId cho ReceiptDetail
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                product.Quantity += dto.QuantityToImport;
                if (dto.SellingPrice.HasValue && dto.SellingPrice.Value > 0)
                {
                    product.Price = dto.SellingPrice.Value;
                }
                // Cập nhật các thông tin khác nếu có gửi lên (Tùy chọn)
                if (!string.IsNullOrEmpty(dto.Description)) product.Description = dto.Description;
                if (dto.SubCategoryId.HasValue) product.SubCategoryId = dto.SubCategoryId;
                
                if (product.Images == null) product.Images = new List<ProductImage>();

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    product.ImageUrl = imageUrl;
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = imageUrl,
                        IsMain = true,
                        DisplayOrder = 0
                    });
                }

                if (additionalImages != null && additionalImages.Any())
                {
                    int order = product.Images.Count > 0 ? product.Images.Max(i => i.DisplayOrder) + 1 : 1;
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

                product.UpdatedAt = DateTime.Now;
                product.UpdatedBy = adminName;
                await _unitOfWork.Products.UpdateAsync(product);
            }

            // 1. Tạo Phiếu Nhập Kho (InventoryReceipt)
            var receipt = new InventoryReceipt
            {
                SupplierId = dto.SupplierId ?? 1, // Giả định ID 1 là NCC Mặc định
                EmployeeId = adminId,
                ReceivedDate = DateTime.Now,
                TotalAmount = dto.QuantityToImport * dto.ImportPrice,
                Notes = dto.Notes ?? (isNewProduct ? "Nhập hàng khởi tạo sản phẩm mới" : "Nhập hàng bổ sung"),
                Status = ReceiptStatus.Completed
            };
            await _unitOfWork.InventoryReceipts.AddAsync(receipt);
            await _unitOfWork.SaveChangesAsync(); // Để lấy ReceiptId

            // 2. Tạo Chi Tiết Phiếu Nhập (InventoryReceiptDetail)
            var detail = new InventoryReceiptDetail
            {
                InventoryReceiptId = receipt.Id,
                ProductId = product.Id,
                Quantity = dto.QuantityToImport,
                ImportPrice = dto.ImportPrice
            };
            await _unitOfWork.InventoryReceiptDetails.AddAsync(detail);

            // 3. Ghi log lịch sử kho (Để hiển thị nhanh ở Dashboard)
            var history = new StockHistory
            {
                ProductId = product.Id,
                ChangeQuantity = dto.QuantityToImport,
                Reason = isNewProduct ? $"Nhập hàng mới qua Phiếu #{receipt.Id}" : $"Nhập bổ sung qua Phiếu #{receipt.Id}",
                CreatedAt = DateTime.Now,
                ChangedBy = adminName
            };
            await _unitOfWork.StockHistories.AddAsync(history);

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<StockHistoryDTO>> GetStockHistoryAsync(int limit = 50)
        {
            var historyList = await _unitOfWork.StockHistories.GetAllAsync();
            var products = await _unitOfWork.Products.GetAllAsync();

            return historyList
                .OrderByDescending(h => h.CreatedAt)
                .Take(limit)
                .Join(products, h => h.ProductId, p => p.Id, (h, p) => new StockHistoryDTO
                {
                    Id = h.Id,
                    ProductName = p.Name,
                    SKU = p.SKU ?? "N/A",
                    ChangeQuantity = h.ChangeQuantity,
                    Reason = h.Reason,
                    CreatedAt = h.CreatedAt,
                    ChangedBy = h.ChangedBy
                }).ToList();
        }

        public async Task<IEnumerable<InventoryReceiptDTO>> GetAllReceiptsAsync()
        {
            var receipts = await _unitOfWork.InventoryReceipts.GetAllAsync();
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var users = await _userManager.Users.ToListAsync();

            return receipts.OrderByDescending(r => r.ReceivedDate).Select(r => new InventoryReceiptDTO
            {
                Id = r.Id,
                SupplierName = suppliers.FirstOrDefault(s => s.Id == r.SupplierId)?.Name ?? "N/A",
                EmployeeName = users.FirstOrDefault(u => u.Id == r.EmployeeId)?.FullName ?? "Admin",
                ReceivedDate = r.ReceivedDate,
                TotalAmount = r.TotalAmount,
                Status = r.Status.ToString(),
                Notes = r.Notes ?? ""
            }).ToList();
        }

        public async Task<InventoryReceiptDTO?> GetReceiptByIdAsync(int id)
        {
            // Giả định UnitOfWork/Repository hỗ trợ Include hoặc lấy details
            var receipts = await _unitOfWork.InventoryReceipts.GetAllAsync();
            var r = receipts.FirstOrDefault(x => x.Id == id);
            if (r == null) return null;

            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var details = await _unitOfWork.InventoryReceiptDetails.GetAllAsync();
            var products = await _unitOfWork.Products.GetAllAsync();

            var users = await _userManager.Users.ToListAsync();
            var employeeName = users.FirstOrDefault(u => u.Id == r.EmployeeId)?.FullName ?? "Admin";

            var receiptDTO = new InventoryReceiptDTO
            {
                Id = r.Id,
                SupplierName = suppliers.FirstOrDefault(s => s.Id == r.SupplierId)?.Name ?? "N/A",
                EmployeeName = employeeName,
                ReceivedDate = r.ReceivedDate,
                TotalAmount = r.TotalAmount,
                Status = r.Status.ToString(),
                Notes = r.Notes ?? ""
            };

            receiptDTO.Details = details.Where(d => d.InventoryReceiptId == r.Id)
                .Join(products, d => d.ProductId, p => p.Id, (d, p) => new InventoryReceiptDetailDTO
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU ?? "N/A",
                    ImageUrl = p.ImageUrl,
                    Quantity = d.Quantity,
                    ImportPrice = d.ImportPrice
                }).ToList();

            // Đảm bảo TotalAmount khớp với tổng Details (Fix lỗi hiển thị sai số liệu)
            receiptDTO.TotalAmount = receiptDTO.Details.Sum(x => x.SubTotal);

            return receiptDTO;
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return products.FirstOrDefault(p => p.SKU == sku);
        }
    }
}
