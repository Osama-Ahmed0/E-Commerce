using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    public class ProductService(AppDbContext context) : IProductService
    {
        private readonly AppDbContext context = context;

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

            int validPageNumber = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
            int validPageSize = pageSize.HasValue && pageSize.Value > 0 ? Math.Min(pageSize.Value, 100) : 20;

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((validPageNumber - 1) * validPageSize)
                .Take(validPageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name
                }).ToListAsync();

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

            return ServiceResult<ProductResponseDto>.Ok(new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name
            });
        }

        public async Task<ServiceResult<ProductResponseDto>> CreateProductAsync(ProductDto dto)
        {
            var category = await context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
                return ServiceResult<ProductResponseDto>.Fail("Category not found", ServiceErrorType.NotFound);

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return ServiceResult<ProductResponseDto>.Ok(new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                CategoryName = category.Name
            });
        }

        public async Task<ServiceResult<ProductResponseDto>> UpdateProductAsync(int id, ProductDto dto)
        {
            if (dto == null)
                return ServiceResult<ProductResponseDto>.Fail("Invalid product data", ServiceErrorType.BadRequest);

            var product = await context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return ServiceResult<ProductResponseDto>.Fail("Product not found", ServiceErrorType.NotFound);

            if (dto.CategoryId != product.CategoryId)
            {
                var category = await context.Categories.FindAsync(dto.CategoryId);
                if (category == null)
                    return ServiceResult<ProductResponseDto>.Fail("Category not found", ServiceErrorType.NotFound);
            }

            product.Name = dto.Name;
            product.Description = dto.Description ?? string.Empty;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;

            await context.SaveChangesAsync();

            return ServiceResult<ProductResponseDto>.Ok(new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name
            });
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