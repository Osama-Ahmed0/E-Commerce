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
    public class ProductService(AppDbContext context, IMapper mapper, IProductValidator validator) : IProductService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;
        private readonly IProductValidator validator = validator;

        public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId, decimal? minPrice,
            decimal? maxPrice, string? sort, int? pageNumber, int? pageSize)
        {
            var query = context.Products.AsNoTracking().AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort?.ToLowerInvariant() switch
            {
                "price_asc" or "+price" => query.OrderBy(p => p.Price),
                "price_desc" or "-price" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Price)
            };

            var (validPageNumber, validPageSize) = PaginationHelper.Normalize(pageNumber, pageSize, defaultPageSize: 20);

            var totalCount = await query.CountAsync();

            var products = await query
                .ProjectTo<ProductResponseDto>(mapper.ConfigurationProvider)
                .Skip((validPageNumber - 1) * validPageSize)
                .Take(validPageSize)
                .ToListAsync();

            return new PagedResult<ProductResponseDto>
            {
                Items = products,
                TotalCount = totalCount,
                Page = validPageNumber,
                PageSize = validPageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / validPageSize)
            };
        }
        public async Task<ServiceResult<ProductResponseDto>> GetProductByIdAsync(int id)
        {
            var product = await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return ServiceResult<ProductResponseDto>.Fail("Product not found", ServiceErrorType.NotFound);

            return ServiceResult<ProductResponseDto>.Ok(mapper.Map<ProductResponseDto>(product));
        }

        public async Task<ServiceResult<ProductResponseDto>> CreateProductAsync(ProductDto dto)
        {
            var validation = await validator.ValidateForCreateAsync(dto);
            if (!validation.IsValid)
                return ServiceResult<ProductResponseDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

            var product = mapper.Map<Product>(dto);

            context.Products.Add(product);
            await context.SaveChangesAsync();

            await context.Entry(product).Reference(p => p.Category).LoadAsync();

            return ServiceResult<ProductResponseDto>.Ok(mapper.Map<ProductResponseDto>(product));
        }

        public async Task<ServiceResult<ProductResponseDto>> UpdateProductAsync(int id, ProductDto dto)
        {
            var product = await context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return ServiceResult<ProductResponseDto>.Fail("Product not found", ServiceErrorType.NotFound);

            var validation = await validator.ValidateForUpdateAsync(dto, product);
            if (!validation.IsValid)
                return ServiceResult<ProductResponseDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

            mapper.Map(dto, product);

            if (product.CategoryId != product.Category.Id)
                await context.Entry(product).Reference(p => p.Category).LoadAsync();

            await context.SaveChangesAsync();

            return ServiceResult<ProductResponseDto>.Ok(mapper.Map<ProductResponseDto>(product));
        }

        public async Task<ServiceResult<bool>> DeleteProductAsync(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
                return ServiceResult<bool>.Fail("Product not found", ServiceErrorType.NotFound);

            context.Products.Remove(product);
            await context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}