using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService service) : ControllerBase
    {
        private readonly ICategoryService service = service;

        [HttpGet]
        public async Task<IActionResult> GetCategories(int? pageNumber, int? pageSize)
        {
            var categories = await service.GetCategoriesAsync(pageNumber, pageSize);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await service.GetCategoryByIdAsync(id);
            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryDto dto)
        {
            var result = await service.CreateCategoryAsync(dto);
            if (!result.Success)
                return result.ToActionResult(this);

            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Data?.Id }, result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDto dto)
        {
            var result = await service.UpdateCategoryAsync(id, dto);
            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await service.DeleteCategoryAsync(id);
            return result.ToActionResult(this);
        }
    }
}
