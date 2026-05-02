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

        public async Task<bool> ImportStockAsync(BulkStockImportDTO bulkDto, string adminName, string? adminId, Dictionary<int, string> mainImages, Dictionary<int, List<string>> galleryImages)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Tạo Phiếu Nhập Kho (InventoryReceipt) trước
                var receipt = new InventoryReceipt
                {
                    SupplierId = bulkDto.SupplierId,
                    EmployeeId = adminId,
                    ReceivedDate = DateTime.Now,
                    TotalAmount = 0, // Sẽ cập nhật sau khi tính tổng details
                    Notes = bulkDto.Notes ?? "Nhập hàng theo lô",
                    Status = ReceiptStatus.Completed
                };
                await _unitOfWork.InventoryReceipts.AddAsync(receipt);
                await _unitOfWork.SaveChangesAsync();

                decimal totalAmount = 0;

                for (int i = 0; i < bulkDto.Items.Count; i++)
                {
                    var item = bulkDto.Items[i];
                    var product = await _unitOfWork.Products.GetBySKUAsync(item.SKU);

                    bool isNewProduct = false;
                    string? imageUrl = mainImages.ContainsKey(i) ? mainImages[i] : null;
                    List<string>? additionalImages = galleryImages.ContainsKey(i) ? galleryImages[i] : null;

                    if (product == null)
                    {
                        isNewProduct = true;
                        product = new Product
                        {
                            SKU = item.SKU,
                            Name = item.Name ?? "Sản phẩm mới",
                            Brand = item.Brand ?? "Unknown",
                            Description = item.Description ?? "",
                            Price = item.SellingPrice ?? 0,
                            Quantity = item.QuantityToImport,
                            CategoryId = item.CategoryId ?? 1,
                            SubCategoryId = item.SubCategoryId,
                            ImageUrl = imageUrl ?? "default_product.png",
                            CreatedAt = DateTime.Now,
                            IsActive = true,
                            CreatedBy = adminName
                        };

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            product.Images.Add(new ProductImage { ImageUrl = imageUrl, IsMain = true, DisplayOrder = 0 });
                        }

                        if (additionalImages != null)
                        {
                            int order = 1;
                            foreach (var img in additionalImages)
                            {
                                product.Images.Add(new ProductImage { ImageUrl = img, IsMain = false, DisplayOrder = order++ });
                            }
                        }

                        await _unitOfWork.Products.AddAsync(product);
                        // SaveChanges ở đây để lấy Product.Id cho các bảng liên quan (Detail, History)
                        await _unitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        product.Quantity += item.QuantityToImport;
                        product.UpdatedAt = DateTime.Now;
                        product.UpdatedBy = adminName;
                        await _unitOfWork.Products.UpdateAsync(product);
                    }

                    var detail = new InventoryReceiptDetail
                    {
                        InventoryReceiptId = receipt.Id,
                        ProductId = product.Id,
                        Quantity = item.QuantityToImport,
                        ImportPrice = item.ImportPrice
                    };
                    await _unitOfWork.InventoryReceiptDetails.AddAsync(detail);
                    totalAmount += item.QuantityToImport * item.ImportPrice;

                    // Ghi log lịch sử
                    await _unitOfWork.StockHistories.AddAsync(new StockHistory
                    {
                        ProductId = product.Id,
                        ChangeQuantity = item.QuantityToImport,
                        Reason = isNewProduct ? $"Nhập hàng mới (Phiếu #{receipt.Id})" : $"Nhập bổ sung (Phiếu #{receipt.Id})",
                        CreatedAt = DateTime.Now,
                        ChangedBy = adminName
                    });
                }

                receipt.TotalAmount = totalAmount;
                await _unitOfWork.SaveChangesAsync();
                
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw; // Để Global Middleware bắt lại
            }
        }

        public async Task<PagedResultDTO<StockHistoryDTO>> GetStockHistoryPagedAsync(int page = 1, int pageSize = 10)
        {
            var historyQuery = _unitOfWork.StockHistories.GetQueryable();
            var productQuery = _unitOfWork.Products.GetQueryable();

            var query = from h in historyQuery
                        join p in productQuery on h.ProductId equals p.Id
                        select new StockHistoryDTO
                        {
                            Id = h.Id,
                            ProductName = p.Name,
                            SKU = p.SKU ?? "N/A",
                            ChangeQuantity = h.ChangeQuantity,
                            Reason = h.Reason,
                            CreatedAt = h.CreatedAt,
                            ChangedBy = h.ChangedBy
                        };

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<StockHistoryDTO>
            {
                Items = items,
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<PagedResultDTO<InventoryReceiptDTO>> GetAllReceiptsPagedAsync(int page = 1, int pageSize = 10)
        {
            var receiptQuery = _unitOfWork.InventoryReceipts.GetQueryable();
            var supplierQuery = _unitOfWork.Suppliers.GetQueryable();
            var userQuery = _userManager.Users.AsQueryable();

            var query = from r in receiptQuery
                        join s in supplierQuery on r.SupplierId equals s.Id
                        join u in userQuery on r.EmployeeId equals u.Id into users
                        from u in users.DefaultIfEmpty()
                        select new InventoryReceiptDTO
                        {
                            Id = r.Id,
                            SupplierName = s.Name,
                            EmployeeName = u != null ? u.FullName : "Admin",
                            ReceivedDate = r.ReceivedDate,
                            TotalAmount = r.TotalAmount,
                            Status = r.Status.ToString(),
                            Notes = r.Notes ?? ""
                        };

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.ReceivedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<InventoryReceiptDTO>
            {
                Items = items,
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<InventoryReceiptDTO?> GetReceiptByIdAsync(int id)
        {
            var receiptQuery = _unitOfWork.InventoryReceipts.GetQueryable();
            var supplierQuery = _unitOfWork.Suppliers.GetQueryable();
            var userQuery = _userManager.Users.AsQueryable();

            var r = await (from rc in receiptQuery
                           join s in supplierQuery on rc.SupplierId equals s.Id
                           join u in userQuery on rc.EmployeeId equals u.Id into users
                           from u in users.DefaultIfEmpty()
                           where rc.Id == id
                           select new InventoryReceiptDTO
                           {
                               Id = rc.Id,
                               SupplierName = s.Name,
                               EmployeeName = u != null ? u.FullName : "Admin",
                               ReceivedDate = rc.ReceivedDate,
                               TotalAmount = rc.TotalAmount,
                               Status = rc.Status.ToString(),
                               Notes = rc.Notes ?? ""
                           }).FirstOrDefaultAsync();

            if (r == null) return null;

            var detailsQuery = _unitOfWork.InventoryReceiptDetails.GetQueryable();
            var productQuery = _unitOfWork.Products.GetQueryable();

            r.Details = await (from d in detailsQuery
                               join p in productQuery on d.ProductId equals p.Id
                               where d.InventoryReceiptId == id
                               select new InventoryReceiptDetailDTO
                               {
                                   ProductId = p.Id,
                                   ProductName = p.Name,
                                   SKU = p.SKU ?? "N/A",
                                   ImageUrl = p.ImageUrl,
                                   Quantity = d.Quantity,
                                   ImportPrice = d.ImportPrice
                               }).ToListAsync();

            r.TotalAmount = r.Details.Sum(x => x.SubTotal);
            return r;
        }

        public async Task<Product?> GetProductBySKUAsync(string sku)
        {
            return await _unitOfWork.Products.GetBySKUAsync(sku);
        }
    }
}
