using AutoMapper;
using AutoMapper.QueryableExtensions;
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

        public async Task<ServiceResult<ReviewDto>> CreateReviewAsync(int productId, string userId, WriteReviewDto Dto)
        {
            if (Dto == null)
                return ServiceResult<ReviewDto>.Fail("Invalid review data", ServiceErrorType.BadRequest);

            var product = await context.Products.FindAsync(productId);
            if (product == null)
                return ServiceResult<ReviewDto>.Fail("Product not found", ServiceErrorType.NotFound);

            var user = await context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<ReviewDto>.Fail("User not found", ServiceErrorType.NotFound);

            var already = await context.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
            if (already)
                return ServiceResult<ReviewDto>.Fail("User has already reviewed this product", ServiceErrorType.Conflict);

            var review = new Review
            {
                Rating = Dto.Rating,
                Comment = Dto.Comment,
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

            var dto = mapper.Map<ReviewDto>(review);

            return ServiceResult<ReviewDto>.Ok(dto);
        }
    }
}
