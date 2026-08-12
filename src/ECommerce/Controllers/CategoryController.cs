using ECommerce.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await service.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await service.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound($"Category with ID {id} was not found.");

            return Ok(category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryDto dto)
        {
            var result = await service.CreateCategoryAsync(dto);
            if (!result)
                return BadRequest("Failed to create category.");

            return Created($"api/categories/{dto.Name}", dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCategory(int id, CategoryDto dto)
        {
            var result = await service.UpdateCategoryAsync(id, dto);
            if (!result)
                return BadRequest("Failed to update category.");

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var result = await service.DeleteCategoryAsync(id);
            if (!result)
                return BadRequest("Failed to delete category.");
            return Ok(result);
        }
    }
}
