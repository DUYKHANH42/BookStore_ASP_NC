using BookStore.Application.DTO;
using BookStore.Application.Services;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookStore.API.Areas.Customer.Controllers
{
    [ApiController]
    [Route("api/products")] 
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDTO<ProductDTO>>> GetAllProducts([FromQuery] ProductQueryParameters query)
        {
            var result = await _productService.GetProductsAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProductByID(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
            }

            return Ok(product);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<PagedResultDTO<ProductDTO>>> GetProductsByCategory(int categoryId, [FromQuery] ProductQueryParameters query)
        {
            query.CategoryId = categoryId;
            var result = await _productService.GetProductsAsync(query);
            return Ok(result);
        }

        [HttpGet("subcategory/{subCategoryId}")]
        public async Task<ActionResult<PagedResultDTO<ProductDTO>>> GetProductsBySubCategory(int subCategoryId, [FromQuery] ProductQueryParameters query)
        {
            query.SubCategoryId = subCategoryId;
            var result = await _productService.GetProductsAsync(query);
            return Ok(result);
        }

        [HttpGet("new-arrivals")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetNewArrivals([FromQuery] int count = 10)
        {
            var products = await _productService.GetNewArrivalsAsync(count);
            return Ok(products);
        }

        [HttpGet("{id}/related")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetRelatedProducts(int id, [FromQuery] int count = 4)
        {
            var products = await _productService.GetRelatedProductsAsync(id, count);
            return Ok(products);
        }

        [HttpGet("flash-sale")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetFlashSale([FromQuery] int count = 10)
        {
            var products = await _productService.GetFlashSaleProductsAsync(count);
            return Ok(products);
        }
    }
}