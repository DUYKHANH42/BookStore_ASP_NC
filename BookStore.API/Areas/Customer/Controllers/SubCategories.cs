using BookStore.Application.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/subcategories")]
    public class SubCategories : Controller
    {
        private readonly Application.Services.SubCategoriesService _services;
        public SubCategories(Application.Services.SubCategoriesService services)
        {
            _services = services;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubCategoryDTO>>> GetAllSubCategories()
        {
            var subcategories = await _services.GetAll();
            return Ok(subcategories);
        }
    }
}
