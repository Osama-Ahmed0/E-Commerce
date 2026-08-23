using AutoMapper;
using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    public class ReviewService(AppDbContext context, IMapper mapper) : IReviewService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;

        public async Task<PagedResult<ReviewDto>> GetReviewsAsync(int productId, int? pageNumber, int? pageSize)
        {
            int validPageNumber = pageNumber.GetValueOrDefault(1);
            int validPageSize = pageSize.GetValueOrDefault(10);

            var query = context.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((validPageNumber - 1) * validPageSize)
                .Take(validPageSize)
                .Select(r => mapper.Map<ReviewDto>(r))
                .ToListAsync();

            return new PagedResult<ReviewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = validPageNumber,
                PageSize = validPageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / validPageSize)
            };
        }

        public async Task<ServiceResult<WriteReviewDto>> CreateReviewAsync(int productId, string userId, WriteReviewDto Dto)
        {
            if (Dto == null)
                return ServiceResult<WriteReviewDto>.Fail("Invalid review data", ServiceErrorType.BadRequest);

            if (Dto.Rating < 1 || Dto.Rating > 5)
                return ServiceResult<WriteReviewDto>.Fail("Rating must be between 1 and 5", ServiceErrorType.Validation);

            var product = await context.Products.FindAsync(productId);
            if (product == null)
                return ServiceResult<WriteReviewDto>.Fail("Product not found", ServiceErrorType.NotFound);

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<WriteReviewDto>.Fail("User not found", ServiceErrorType.NotFound);

            var already = await context.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
            if (already)
                return ServiceResult<WriteReviewDto>.Fail("User has already reviewed this product", ServiceErrorType.Conflict);

            var review = new Review
            {
                Rating = Dto.Rating,
                Comment = Dto.Comment?.Trim() ?? string.Empty,
                ProductId = productId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Reviews.Add(review);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ServiceResult<WriteReviewDto>.Fail("Could not create review.", ServiceErrorType.Conflict);
            }

            return ServiceResult<WriteReviewDto>.Ok(new WriteReviewDto { Rating = review.Rating, Comment = review.Comment });
        }
    }
}
