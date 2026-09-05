using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    public class ReviewService(AppDbContext context, IMapper mapper, IReviewValidator validator) : IReviewService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;
        private readonly IReviewValidator validator = validator;

        public async Task<PagedResult<ReviewDto>> GetReviewsAsync(int productId, int? pageNumber, int? pageSize)
        {
            var (validPageNumber, validPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var query = context.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .ProjectTo<ReviewDto>(mapper.ConfigurationProvider)
                .Skip((validPageNumber - 1) * validPageSize)
                .Take(validPageSize)
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

        public async Task<ServiceResult<ReviewDto>> CreateReviewAsync(int productId, string userId, WriteReviewDto dto)
        {
            var validation = await validator.ValidateForCreateAsync(productId, userId, dto);
            if (!validation.IsValid)
                return ServiceResult<ReviewDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<ReviewDto>.Fail("User not found", ServiceErrorType.NotFound);

            var review = new Review
            {
                Rating = dto.Rating,
                Comment = dto.Comment ?? string.Empty,
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
                return ServiceResult<ReviewDto>.Fail("Could not create review.", ServiceErrorType.Conflict);
            }

            review.User = user;

            var reviewDto = mapper.Map<ReviewDto>(review);

            return ServiceResult<ReviewDto>.Ok(reviewDto);
        }
    }
}
