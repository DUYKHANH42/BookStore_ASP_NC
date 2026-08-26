using BookStore.Application.DTO;
using BookStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/addresses")]
    public class ShippingAddressesController : ControllerBase
    {
        private readonly ShippingAddressService _service;
        public ShippingAddressesController(ShippingAddressService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetUserAddressesAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

        [HttpPost]
        public async Task<IActionResult> Create(AddressDTO dto)
        {
            await _service.AddAddressAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, dto);
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Update(AddressDTO dto)
        {
            await _service.UpdateAddressAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAddressAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, id);
            return Ok();
        }
    }
}
