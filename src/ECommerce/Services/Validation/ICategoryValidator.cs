using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public interface ICategoryValidator
    {
        Task<ValidationResult> ValidateForCreateAsync(CategoryDto dto);
        Task<ValidationResult> ValidateForUpdateAsync(CategoryDto dto);
    }
}
