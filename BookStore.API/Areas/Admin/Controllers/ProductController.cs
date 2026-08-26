#nullable enable
using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly CategoriesService _categoriesService;
        private readonly SubCategoriesService _subCategoriesService;
        private readonly BookStore.Application.Interfaces.IFileService _fileService;
        private readonly IActivityLogService _activityLog;

        public ProductController(
            ProductService productService,
            CategoriesService categoriesService,
            SubCategoriesService subCategoriesService,
            BookStore.Application.Interfaces.IFileService fileService,
            IActivityLogService activityLog)
        {
            _productService = productService;
            _categoriesService = categoriesService;
            _subCategoriesService = subCategoriesService;
            _fileService = fileService;
            _activityLog = activityLog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _categoriesService.GetAll();
            ViewBag.SubCategories = await _subCategoriesService.GetAll();
            
            var parameters = new ProductQueryParameters { PageNumber = 1, PageSize = 10 };
            var result = await _productService.GetProductsAsync(parameters);
            
            return View(result);
        }

        [HttpGet("GetProduct/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Json(product);
        }

        [HttpPost("Upsert")]
        public async Task<IActionResult> Upsert([FromForm] int? id, [FromForm] ProductCreateDTO dto, IFormFile? image, List<IFormFile>? additionalImages)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ", errors = errors });
            }

            try
            {
                string? imageUrl = null;

                if (image != null)
                {
                    // Cloudinary will automatically save and return Secure URL
                    imageUrl = await _fileService.SaveFileAsync(image, "products/main");
                }

                var additionalImageUrls = new List<string>();
                if (additionalImages != null && additionalImages.Any())
                {
                    foreach (var img in additionalImages)
                    {
                        var imgUrl = await _fileService.SaveFileAsync(img, "products/gallery");
                        additionalImageUrls.Add(imgUrl);
                    }
                }

                bool result;
                string userName = User.Identity?.Name ?? "Admin";
                var isCreate = id == null || id == 0;
                ProductDTO? oldProduct = null;

                if (isCreate)
                {
                    dto.Quantity = 0;
                    if (string.IsNullOrEmpty(imageUrl)) imageUrl = "default_product.png";
                    result = await _productService.CreateProductAsync(dto, imageUrl, userName, additionalImageUrls);
                }
                else
                {
                    var productId = id.GetValueOrDefault();
                    oldProduct = await _productService.GetProductByIdAsync(productId);
                    if (oldProduct == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                    dto.Quantity = oldProduct.Quantity;
                    result = await _productService.UpdateProductAsync(productId, dto, imageUrl, userName, additionalImageUrls);
                }

                if (result)
                {
                    var details = isCreate
                        ? $"Tạo sản phẩm '{dto.Name}' (SKU: {dto.SKU}) với tồn kho ban đầu 0. Tồn kho sẽ được cập nhật qua phiếu nhập/POS/đơn hàng."
                        : BuildProductChangeLog(oldProduct!, dto, imageUrl != null, additionalImageUrls.Any());

                    await _activityLog.LogAsync(
                        ActivityModules.Product,
                        isCreate ? ActivityActions.Create : ActivityActions.Update,
                        details,
                        entityType: "Product",
                        entityId: isCreate ? dto.SKU : oldProduct?.Id.ToString());
                    return Json(new { success = true, message = "Lưu sản phẩm thành công!" });
                }
                return Json(new { success = false, message = "Không thể lưu sản phẩm." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetProductList")]
        public async Task<IActionResult> GetProductList(ProductQueryParameters parameters)
        {
            var result = await _productService.GetProductsAsync(parameters);
            return PartialView("_ProductListPartial", result);
        }

        [HttpGet("GetSubCategories")]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            if (categoryId <= 0)
                return Json(Array.Empty<object>());

            var subCategories = await _subCategoriesService.GetSubCategoriesByCategoryIdAsync(categoryId);
            return Json(subCategories.Select(s => new { id = s.Id, name = s.Name, categoryId = s.CategoryId }));
        }

        [HttpPost("DeleteImage")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var result = await _productService.DeleteImageAsync(imageId);
            if (!result) return BadRequest(new { success = false, message = "Không thể xóa ảnh." });
            await _activityLog.LogAsync(
                ActivityModules.Product,
                ActivityActions.Delete,
                $"Xóa ảnh phụ sản phẩm (ImageId: {imageId})",
                entityType: "ProductImage",
                entityId: imageId.ToString());
            return Json(new { success = true, message = "Đã xóa ảnh thành công." });
        }

        [HttpPost("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var result = await _productService.ToggleStatusAsync(id);
            if (!result) return NotFound();
            await _activityLog.LogAsync(
                ActivityModules.Product,
                ActivityActions.StatusChange,
                $"Bật/tắt trạng thái sản phẩm '{product.Name}' (ID: {id})",
                entityType: "Product",
                entityId: id.ToString());
            return Json(new { success = true });
        }

        private static string BuildProductChangeLog(ProductDTO oldProduct, ProductCreateDTO dto, bool changedMainImage, bool changedGallery)
        {
            var changes = new List<string>();

            AddChange(changes, "Tên sản phẩm", oldProduct.Name, dto.Name);
            AddChange(changes, "Thương hiệu/Tác giả", oldProduct.Brand, dto.Brand);
            AddChange(changes, "SKU", oldProduct.SKU ?? "", dto.SKU ?? "");
            AddChange(changes, "Mô tả", oldProduct.Description ?? "", dto.Description ?? "");
            AddChange(changes, "Giá bán", $"{oldProduct.Price:N0}đ", $"{dto.Price:N0}đ");
            AddChange(changes, "Danh mục chính", oldProduct.CategoryId.ToString(), dto.CategoryId.ToString());
            AddChange(changes, "Danh mục phụ", oldProduct.SubCategoryId?.ToString() ?? "Không có", dto.SubCategoryId?.ToString() ?? "Không có");

            if (changedMainImage) changes.Add("Thay đổi ảnh đại diện");
            if (changedGallery) changes.Add("Cập nhật bộ ảnh phụ");

            if (changes.Count == 0)
                changes.Add("Không có trường dữ liệu chính thay đổi");

            return $"Cập nhật sản phẩm '{oldProduct.Name}' (ID: {oldProduct.Id}, SKU: {oldProduct.SKU}): {string.Join("; ", changes)}. Tồn kho giữ nguyên {oldProduct.Quantity}.";
        }

        private static void AddChange(List<string> changes, string label, string oldValue, string newValue)
        {
            oldValue = oldValue?.Trim() ?? "";
            newValue = newValue?.Trim() ?? "";
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                changes.Add($"{label} đổi từ '{oldValue}' thành '{newValue}'");
        }
    }
}
