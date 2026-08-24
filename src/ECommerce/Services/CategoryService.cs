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
    public class CategoryService(AppDbContext context, IMapper mapper, ICategoryValidator validator) : ICategoryService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;
        private readonly ICategoryValidator validator = validator;

        public async Task<PagedResult<CategoryResponseDto>> GetCategoriesAsync(int? pageNumber, int? pageSize)
        {
            var (validPageNumber, validPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var totalCount = await context.Categories.CountAsync();

            var items = await context.Categories
            .AsNoTracking()
            .ProjectTo<CategoryResponseDto>(mapper.ConfigurationProvider)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync();

            return new PagedResult<CategoryResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = validPageNumber,
                PageSize = validPageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / validPageSize)
            };
        }

        public async Task<ServiceResult<CategoryResponseDto>> GetCategoryByIdAsync(int id)
        {
            var category = await context.Categories
                .AsNoTracking()
                .ProjectTo<CategoryResponseDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return ServiceResult<CategoryResponseDto>.Fail("Category not found", ServiceErrorType.NotFound);

            return ServiceResult<CategoryResponseDto>.Ok(category);
        }

        public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CategoryDto dto)
        {
            var validation = await validator.ValidateForCreateAsync(dto);
            if (!validation.IsValid)
                return ServiceResult<CategoryResponseDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

            var category = mapper.Map<Category>(dto);

            await context.Categories.AddAsync(category);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ServiceResult<CategoryResponseDto>.Fail("Category with this name already exists", ServiceErrorType.Conflict);
            }

            return ServiceResult<CategoryResponseDto>.Ok(mapper.Map<CategoryResponseDto>(category));
        }

        public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return ServiceResult<CategoryResponseDto>.Fail("Category not found", ServiceErrorType.NotFound);

            var validation = await validator.ValidateForUpdateAsync(id, dto);
            if (!validation.IsValid)
                return ServiceResult<CategoryResponseDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

            mapper.Map(dto, category);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ServiceResult<CategoryResponseDto>.Fail("Category with this name already exists", ServiceErrorType.Conflict);
            }

            return ServiceResult<CategoryResponseDto>.Ok(mapper.Map<CategoryResponseDto>(category));
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return ServiceResult<bool>.Fail("Category not found", ServiceErrorType.NotFound);

            var hasChild = await context.Categories.AnyAsync(c => c.ParentCategoryId == id);
            if (hasChild)
                return ServiceResult<bool>.Fail("Cannot delete category with child nodes", ServiceErrorType.BadRequest);

            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}