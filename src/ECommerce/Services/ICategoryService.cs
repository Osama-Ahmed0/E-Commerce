using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync();
        Task<CategoryResponseDto?> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(CategoryDto dto);
        Task<bool> UpdateCategoryAsync(int id, CategoryDto dto);
        Task<bool> DeleteCategoryAsync(int id);
    }
}