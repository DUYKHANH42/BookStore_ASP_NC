using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class InventoryController : Controller
    {
        private readonly InventoryService _inventoryService;
        private readonly CategoriesService _categoriesService;
        private readonly SubCategoriesService _subCategoriesService;
        private readonly SuppliersService _suppliersService;
        private readonly InvoiceService _invoiceService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IActivityLogService _activityLog;

        public InventoryController(
            InventoryService inventoryService, 
            CategoriesService categoriesService,
            SubCategoriesService subCategoriesService,
            SuppliersService suppliersService,
            InvoiceService invoiceService,
            IWebHostEnvironment webHostEnvironment,
            IActivityLogService activityLog)
        {
            _inventoryService = inventoryService;
            _categoriesService = categoriesService;
            _subCategoriesService = subCategoriesService;
            _suppliersService = suppliersService;
            _invoiceService = invoiceService;
            _webHostEnvironment = webHostEnvironment;
            _activityLog = activityLog;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var receipt = await _inventoryService.GetReceiptByIdAsync(id);
            if (receipt == null) return NotFound();

            string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "imgs", "logo_sach.png");
            var pdfBytes = _invoiceService.GenerateInventoryReceiptPdf(receipt, logoPath);

            return File(pdfBytes, "application/pdf", $"Receipt_PNK_{id}.pdf");
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Hiển thị danh sách Phiếu nhập thay vì chỉ Log lẻ
            var receipts = await _inventoryService.GetAllReceiptsPagedAsync(page, pageSize);
            return View(receipts);
        }

        public async Task<IActionResult> History(int page = 1, int pageSize = 10)
        {
            var history = await _inventoryService.GetStockHistoryPagedAsync(page, pageSize);
            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> GetReceiptDetails(int id)
        {
            var receipt = await _inventoryService.GetReceiptByIdAsync(id);
            if (receipt == null) return NotFound();
            return PartialView("_ReceiptDetailPartial", receipt);
        }

        [HttpGet]
        public async Task<IActionResult> Import()
        {
            ViewBag.Categories = await _categoriesService.GetAll();
            ViewBag.SubCategories = await _subCategoriesService.GetAll();
            ViewBag.Suppliers = await _suppliersService.GetAllAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(BulkStockImportDTO dto)
        {
            if (!ModelState.IsValid) 
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ", errors = errors });
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var mainImages = new Dictionary<int, string>();
            var galleryImages = new Dictionary<int, List<string>>();

            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                
                // Xử lý ảnh chính cho từng item
                if (item.ImageFile != null)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + item.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await item.ImageFile.CopyToAsync(fileStream);
                    }
                    mainImages[i] = uniqueFileName;
                }

                // Xử lý ảnh phụ cho từng item
                if (item.AdditionalImageFiles != null && item.AdditionalImageFiles.Any())
                {
                    var imgList = new List<string>();
                    foreach (var img in item.AdditionalImageFiles)
                    {
                        var imgUrl = Guid.NewGuid().ToString() + "_" + img.FileName;
                        string filePath = Path.Combine(uploadsFolder, imgUrl);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await img.CopyToAsync(fileStream);
                        }
                        imgList.Add(imgUrl);
                    }
                    galleryImages[i] = imgList;
                }
            }

            var adminName = User.Identity?.Name ?? "Admin";
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var result = await _inventoryService.ImportStockAsync(dto, adminName, adminId, mainImages, galleryImages);
                if (result)
                {
                    var suppliers = await _suppliersService.GetAllAsync();
                    var supplier = suppliers.FirstOrDefault(s => s.Id == dto.SupplierId);
                    var totalAmount = dto.Items.Sum(i => i.QuantityToImport * i.ImportPrice);
                    var itemSummary = string.Join("; ", dto.Items.Select(i =>
                        $"{i.SKU}: SL {i.QuantityToImport}, giá nhập {i.ImportPrice:N0}đ"));

                    await _activityLog.LogAsync(
                        ActivityModules.Inventory,
                        ActivityActions.Import,
                        $"Nhập kho {dto.Items?.Count ?? 0} dòng hàng từ NCC '{supplier?.Name ?? $"#{dto.SupplierId}"}'. Chi tiết: {itemSummary}. Tổng tiền phiếu: {totalAmount:N0}đ.",
                        entityType: "InventoryReceipt",
                        actorId: adminId,
                        actorName: adminName);
                    return Json(new { success = true, message = "Nhập kho thành công!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi nhập kho" });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int? categoryId = null, int? parentId = null)
        {
            var catId = categoryId ?? parentId;
            if (!catId.HasValue || catId.Value <= 0)
                return Json(Array.Empty<object>());

            var subCats = await _subCategoriesService.GetSubCategoriesByCategoryIdAsync(catId.Value);
            return Json(subCats.Select(s => new { id = s.Id, name = s.Name, categoryId = s.CategoryId }));
        }

        private static string ToUploadUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
            var clean = url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase);
            return "/uploads/" + clean;
        }

        [HttpGet]
        public async Task<IActionResult> CheckSKU(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return Json(new { exists = false });

            var product = await _inventoryService.GetProductBySKUAsync(sku.Trim());
            if (product != null)
            {
                var categoryId = product.CategoryId;
                if (product.SubCategory != null)
                    categoryId = product.SubCategory.CategoryId;

                return Json(new { 
                    exists = true, 
                    name = product.Name, 
                    brand = product.Brand,
                    price = product.Price,
                    categoryId,
                    subCategoryId = product.SubCategoryId,
                    description = product.Description ?? "",
                    imageUrl = ToUploadUrl(product.ImageUrl),
                    images = product.Images.Select(i => ToUploadUrl(i.ImageUrl)).ToList()
                });
            }
            return Json(new { exists = false });
        }
    }
}
