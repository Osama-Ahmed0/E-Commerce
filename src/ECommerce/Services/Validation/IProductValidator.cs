using ECommerce.Data.Models;
using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public interface IProductValidator
    {
        Task<ValidationResult> ValidateForCreateAsync(ProductDto dto);
        Task<ValidationResult> ValidateForUpdateAsync(ProductDto dto, Product existingProduct);
    }
}
