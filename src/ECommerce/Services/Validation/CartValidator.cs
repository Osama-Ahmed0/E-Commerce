using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public class CartValidator(AppDbContext context) : ICartValidator
    {
        private readonly AppDbContext context = context;

        public async Task<ValidationResult> ValidateForAddAsync(CartItemDto dto)
        {
            if (dto.Quantity <= 0)
                return ValidationResult.Invalid("Quantity must be greater than zero.", ServiceErrorType.Validation);

            var productExists = await context.Products.FindAsync(dto.ProductId);
            if (productExists == null)
                return ValidationResult.Invalid($"Product {dto.ProductId} not found.", ServiceErrorType.NotFound);

            if (dto.Quantity > productExists.Stock)
                return ValidationResult.Invalid($"Quantity exceeds available stock for product {dto.ProductId}.", ServiceErrorType.Validation);

            return ValidationResult.Valid();
        }

        public async Task<ValidationResult> ValidateForUpdateAsync(int productId, int quantity)
        {
            if (quantity <= 0)
                return ValidationResult.Invalid("Quantity must be greater than zero.", ServiceErrorType.Validation);

            var productExists = await context.Products.FindAsync(productId);
            if (productExists == null)
                return ValidationResult.Invalid($"Product {productId} not found.", ServiceErrorType.NotFound);

            if (quantity > productExists.Stock)
                return ValidationResult.Invalid($"Quantity exceeds available stock for product {productId}.", ServiceErrorType.Validation);

            return ValidationResult.Valid();
        }
    }
}
