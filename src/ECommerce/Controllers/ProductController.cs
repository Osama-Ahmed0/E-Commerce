using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService service, IOutputCacheStore cacheStore) : ControllerBase
    {
        private readonly IProductService service = service;
        private readonly IOutputCacheStore cacheStore = cacheStore;

        [HttpGet]
        [OutputCache(PolicyName = "Products")]
        public async Task<IActionResult> GetProducts([FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? search,
            [FromQuery] string? sort, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var result = await service.GetProductsAsync(categoryId, minPrice, maxPrice, search, sort, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [OutputCache(PolicyName = "Products")]
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

            await cacheStore.EvictByTagAsync("products", default);

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data?.Id }, result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpdateProduct(ProductDto productDto)
        {
            var result = await service.UpdateProductAsync(productDto);

            await cacheStore.EvictByTagAsync("products", default);

            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await service.DeleteProductAsync(id);

            if (!result.Success)
                return result.ToActionResult(this);

            await cacheStore.EvictByTagAsync("products", default);

            return result.ToActionResult(this);
        }
    }
}
