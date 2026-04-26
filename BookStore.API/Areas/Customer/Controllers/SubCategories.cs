using BookStore.Application.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/subcategories")]
    public class SubCategories : Controller
    {
        private readonly Application.Services.SubCategories _services;
        public SubCategories(Application.Services.SubCategories services)
        {
            _services = services;
        }
        [HttpGet]
        public async Task<ActionResult<CategoryDTO>> GetAllSubCategories()
        {
            var subcategories = await _services.GetAll();
            return Ok(subcategories);

        }
    }
}
