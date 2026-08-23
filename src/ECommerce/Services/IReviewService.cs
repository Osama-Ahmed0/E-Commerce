using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IReviewService
    {
        Task<PagedResult<ReviewDto>> GetReviewsAsync(int productId, int? pageNumber, int? pageSize);
        Task<ServiceResult<WriteReviewDto>> CreateReviewAsync(int productId, string userId, WriteReviewDto Dto);
    }
}
