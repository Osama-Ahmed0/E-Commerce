using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public interface ICartValidator
    {
        Task<ValidationResult> ValidateForAddAsync(CartItemDto dto);
        Task<ValidationResult> ValidateForUpdateAsync(int productId, int quantity);
    }
}
