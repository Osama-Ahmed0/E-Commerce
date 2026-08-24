using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services.Validation
{
    public class ProductValidator(AppDbContext context) : IProductValidator
    {
        private readonly AppDbContext context = context;

        public async Task<ValidationResult> ValidateForCreateAsync(ProductDto dto)
        {
            if (!await CategoryExistsAsync(dto.CategoryId))
                return ValidationResult.Invalid("Category not found", ServiceErrorType.NotFound);

            return ValidationResult.Valid();
        }

        public async Task<ValidationResult> ValidateForUpdateAsync(ProductDto dto, Product existingProduct)
        {
            if (dto.CategoryId != existingProduct.CategoryId && !await CategoryExistsAsync(dto.CategoryId))
                return ValidationResult.Invalid("Category not found", ServiceErrorType.NotFound);

            return ValidationResult.Valid();
        }

        private async Task<bool> CategoryExistsAsync(int categoryId) =>
            await context.Categories.AnyAsync(c => c.Id == categoryId);
    }
}
