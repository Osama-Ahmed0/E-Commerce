using ECommerce.Common;
using ECommerce.Dtos;
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
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ?
                    NotFound(result.ErrorMessage) :
                    BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductDto productDto)
        {
            var result = await service.CreateProductAsync(productDto);
            if (!result.Success)
                return NotFound(result.ErrorMessage);

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data?.Id }, result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto productDto)
        {
            var result = await service.UpdateProductAsync(id, productDto);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ?
                    NotFound(result.ErrorMessage) :
                    BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await service.DeleteProductAsync(id);
            if (!result.Success)
                return NotFound(result.ErrorMessage);
            return Ok();
        }
    }
}
