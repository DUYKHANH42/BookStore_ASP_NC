using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;

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

        public InventoryController(
            InventoryService inventoryService, 
            CategoriesService categoriesService,
            SubCategoriesService subCategoriesService,
            SuppliersService suppliersService,
            InvoiceService invoiceService,
            IWebHostEnvironment webHostEnvironment)
        {
            _inventoryService = inventoryService;
            _categoriesService = categoriesService;
            _subCategoriesService = subCategoriesService;
            _suppliersService = suppliersService;
            _invoiceService = invoiceService;
            _webHostEnvironment = webHostEnvironment;
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

        public async Task<IActionResult> Index()
        {
            // Hiển thị danh sách Phiếu nhập thay vì chỉ Log lẻ
            var receipts = await _inventoryService.GetAllReceiptsAsync();
            return View(receipts);
        }

        public async Task<IActionResult> History()
        {
            // Trang xem lịch sử biến động kho (Log lẻ)
            var history = await _inventoryService.GetStockHistoryAsync();
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
            ViewBag.Suppliers = await _suppliersService.GetAllAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(StockImportDTO dto)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            string? imageUrl = null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

            if (dto.ImageFile != null)
            {
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + dto.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }
                imageUrl = uniqueFileName;
            }

            var additionalImageUrls = new System.Collections.Generic.List<string>();
            if (dto.AdditionalImageFiles != null && dto.AdditionalImageFiles.Any())
            {
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                foreach (var img in dto.AdditionalImageFiles)
                {
                    var imgUrl = Guid.NewGuid().ToString() + "_" + img.FileName;
                    string filePath = Path.Combine(uploadsFolder, imgUrl);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await img.CopyToAsync(fileStream);
                    }
                    additionalImageUrls.Add(imgUrl);
                }
            }

            var adminName = User.Identity?.Name ?? "Admin";
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var result = await _inventoryService.ImportStockAsync(dto, adminName, adminId, imageUrl, additionalImageUrls);
                if (result) return Json(new { success = true, message = "Nhập kho thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi nhập kho" });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var subCats = await _subCategoriesService.GetSubCategoriesByCategoryIdAsync(categoryId);
            return Json(subCats.Select(s => new { id = s.Id, name = s.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> CheckSKU(string sku)
        {
            var product = await _inventoryService.GetProductBySKUAsync(sku);
            if (product != null)
            {
                return Json(new { 
                    exists = true, 
                    name = product.Name, 
                    brand = product.Brand,
                    price = product.Price,
                    categoryId = product.CategoryId,
                    subCategoryId = product.SubCategoryId,
                    description = product.Description
                });
            }
            return Json(new { exists = false });
        }
    }
}
