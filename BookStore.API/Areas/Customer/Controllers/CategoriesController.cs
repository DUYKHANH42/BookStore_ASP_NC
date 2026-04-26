using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : Controller
    {
        private readonly CategoriesService _services;
        public CategoriesController(CategoriesService services)
        {
            _services = services;
        }
        [HttpGet]
        public async Task<ActionResult<SubCategoryDTO>> GetAllCategories()
        {
            var categories = await _services.GetAll();
            return Ok(categories);
        }
    }
}
