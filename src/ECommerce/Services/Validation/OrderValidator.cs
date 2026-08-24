using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public class OrderValidator : IOrderValidator
    {
        public async Task<ValidationResult> ValidateForCreateAsync(CheckoutRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.ShippingAddress))
                return ValidationResult.Invalid("Shipping address is required.", ServiceErrorType.BadRequest);
            return ValidationResult.Valid();
        }

        public async Task<ValidationResult> ValidateForUpdateStatusAsync(UpdateOrderStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OrderStatus))
                return ValidationResult.Invalid("Status is required.", ServiceErrorType.Validation);

            if (!Enum.TryParse<OrderStatus>(dto.OrderStatus, true, out var requested))
                return ValidationResult.Invalid("Invalid order status.", ServiceErrorType.Validation);

            return ValidationResult.Valid();
        }
    }
}
