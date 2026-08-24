using ECommerce.Dtos;

namespace ECommerce.Services.Validation
{
    public interface IReviewValidator
    {
        Task<ValidationResult> ValidateForCreateAsync(int productId, string userId, WriteReviewDto dto);
    }
}
