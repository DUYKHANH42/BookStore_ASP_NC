using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class CategoryController : Controller
    {
        private readonly CategoriesService _categoryService;
        private readonly SubCategoriesService _subCategoryService;

        public CategoryController(CategoriesService categoryService, SubCategoriesService subCategoryService)
        {
            _categoryService = categoryService;
            _subCategoryService = subCategoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAll();
            return View(categories);
        }

        public async Task<IActionResult> GetCategoryList()
        {
            var categories = await _categoryService.GetAll();
            return PartialView("_CategoryListPartial", categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryService.GetById(id);
            return Json(category);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertCategory(CategoryDTO dto)
        {
            if (dto.Id == 0)
            {
                await _categoryService.CreateAsync(dto);
            }
            else
            {
                await _categoryService.UpdateAsync(dto.Id, dto);
            }
            return Json(new { success = true, message = "Lưu danh mục thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (result == "success")
                return Json(new { success = true, message = "Xóa danh mục thành công" });
            
            return Json(new { success = false, message = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategory = await _subCategoryService.GetById(id);
            return Json(subCategory);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertSubCategory(SubCategoryDTO dto)
        {
            if (dto.Id == 0)
            {
                await _subCategoryService.CreateAsync(dto);
            }
            else
            {
                await _subCategoryService.UpdateAsync(dto.Id, dto);
            }
            return Json(new { success = true, message = "Lưu danh mục phụ thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var result = await _subCategoryService.DeleteAsync(id);
            if (result == "success")
                return Json(new { success = true, message = "Xóa danh mục phụ thành công" });
            
            return Json(new { success = false, message = result });
        }
    }
}
