using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public interface IOrderValidator
    {
        Task<ValidationResult> ValidateForCreateAsync(CheckoutRequestDto dto);
        Task<ValidationResult> ValidateForUpdateStatusAsync(UpdateOrderStatusDto dto);
    }
}
