using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services.Validation
{
    public class ReviewValidator(AppDbContext context) : IReviewValidator
    {
        private readonly AppDbContext context = context;

        public async Task<ValidationResult> ValidateForCreateAsync(int productId, string userId, WriteReviewDto dto)
        {
            var product = await context.Products.FindAsync(productId);
            if (product == null)
                return ValidationResult.Invalid("Product not found", ServiceErrorType.NotFound);

            var already = await context.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
            if (already)
                return ValidationResult.Invalid("User has already reviewed this product", ServiceErrorType.Conflict);

            return ValidationResult.Valid();
        }
    }
}
