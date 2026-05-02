#nullable enable
using BookStore.Application.DTO;
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
        private readonly IWebHostEnvironment _hostingEnv;

        public ProductController(
            ProductService productService,
            CategoriesService categoriesService,
            SubCategoriesService subCategoriesService,
            IWebHostEnvironment hostingEnv)
        {
            _productService = productService;
            _categoriesService = categoriesService;
            _subCategoriesService = subCategoriesService;
            _hostingEnv = hostingEnv;
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
                return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
            }

            try
            {
                string? imageUrl = null;
                string uploadsFolder = Path.Combine(_hostingEnv.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (image != null)
                {
                    imageUrl = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, imageUrl);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }
                }

                var additionalImageUrls = new System.Collections.Generic.List<string>();
                if (additionalImages != null && additionalImages.Any())
                {
                    foreach (var img in additionalImages)
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

                bool result;
                string userName = User.Identity?.Name ?? "Admin";

                if (id == null || id == 0)
                {
                    if (string.IsNullOrEmpty(imageUrl)) imageUrl = "default_product.png";
                    result = await _productService.CreateProductAsync(dto, imageUrl, userName, additionalImageUrls);
                }
                else
                {
                    result = await _productService.UpdateProductAsync(id.Value, dto, imageUrl, userName, additionalImageUrls);
                }

                if (result) return Json(new { success = true, message = "Lưu sản phẩm thành công!" });
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

        [HttpPost("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _productService.ToggleStatusAsync(id);
            if (!result) return NotFound();
            return Json(new { success = true });
        }
    }
}
