using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService service) : ControllerBase
    {
        private readonly IProductService service = service;

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
            [FromQuery] string? sort, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var result = await service.GetProductsAsync(categoryId, minPrice, maxPrice, sort, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await service.GetProductByIdAsync(id);
            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductDto productDto)
        {
            var result = await service.CreateProductAsync(productDto);
            if (!result.Success)
                return result.ToActionResult(this);

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data?.Id }, result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto productDto)
        {
            var result = await service.UpdateProductAsync(id, productDto);
            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await service.DeleteProductAsync(id);
            return result.ToActionResult(this);
        }
    }
}
