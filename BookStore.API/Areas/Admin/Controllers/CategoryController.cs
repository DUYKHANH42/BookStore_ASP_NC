using BookStore.Application.DTO;
using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Cookies")]
    public class CategoryController : Controller
    {
        private readonly CategoriesService _categoryService;
        private readonly SubCategoriesService _subCategoryService;
        private readonly IActivityLogService _activityLog;

        public CategoryController(
            CategoriesService categoryService,
            SubCategoriesService subCategoryService,
            IActivityLogService activityLog)
        {
            _categoryService = categoryService;
            _subCategoryService = subCategoryService;
            _activityLog = activityLog;
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
            }

            var isCreate = dto.Id == 0;
            if (isCreate)
                await _categoryService.CreateAsync(dto);
            else
                await _categoryService.UpdateAsync(dto.Id, dto);

            await _activityLog.LogAsync(
                ActivityModules.Category,
                isCreate ? ActivityActions.Create : ActivityActions.Update,
                $"{(isCreate ? "Tạo" : "Cập nhật")} danh mục '{dto.Name}'",
                entityType: "Category",
                entityId: dto.Id.ToString());
            return Json(new { success = true, message = "Lưu danh mục thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (result == "success")
            {
                await _activityLog.LogAsync(
                    ActivityModules.Category,
                    ActivityActions.Delete,
                    $"Xóa danh mục #{id}",
                    entityType: "Category",
                    entityId: id.ToString());
                return Json(new { success = true, message = "Xóa danh mục thành công" });
            }
            
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
            }

            var isCreate = dto.Id == 0;
            if (isCreate)
                await _subCategoryService.CreateAsync(dto);
            else
                await _subCategoryService.UpdateAsync(dto.Id, dto);

            await _activityLog.LogAsync(
                ActivityModules.Category,
                isCreate ? ActivityActions.Create : ActivityActions.Update,
                $"{(isCreate ? "Tạo" : "Cập nhật")} danh mục phụ '{dto.Name}'",
                entityType: "SubCategory",
                entityId: dto.Id.ToString());
            return Json(new { success = true, message = "Lưu danh mục phụ thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var result = await _subCategoryService.DeleteAsync(id);
            if (result == "success")
            {
                await _activityLog.LogAsync(
                    ActivityModules.Category,
                    ActivityActions.Delete,
                    $"Xóa danh mục phụ #{id}",
                    entityType: "SubCategory",
                    entityId: id.ToString());
                return Json(new { success = true, message = "Xóa danh mục phụ thành công" });
            }
            
            return Json(new { success = false, message = result });
        }
        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var allSub = await _subCategoryService.GetAll();
            // Lọc lấy danh sách sub thuộc về categoryId truyền lên
            var filtered = allSub.Where(s => s.CategoryId == categoryId)
                         .Select(s => new { id = s.Id, name = s.Name })
                         .ToList();
            return Json(filtered);
        }
    }
}
