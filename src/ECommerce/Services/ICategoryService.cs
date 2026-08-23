using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface ICategoryService
    {
        Task<PagedResult<CategoryResponseDto>> GetCategoriesAsync(int? pageNumber, int? pageSize);
        Task<ServiceResult<CategoryResponseDto?>> GetCategoryByIdAsync(int id);
        Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(int id, CategoryDto dto);
        Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CategoryDto dto);
        Task<ServiceResult<bool>> DeleteCategoryAsync(int id);
    }
}