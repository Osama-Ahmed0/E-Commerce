using AutoMapper;
using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    public class CategoryService(AppDbContext context, IMapper mapper) : ICategoryService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;

        public async Task<PagedResult<CategoryResponseDto>> GetCategoriesAsync(int? pageNumber, int? pageSize)
        {
            var (validPageNumber, validPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

            var totalCount = await context.Categories.CountAsync();

            var items = await context.Categories
            .AsNoTracking()
            .Select(c => mapper.Map<CategoryResponseDto>(c))
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
        public async Task<ServiceResult<CategoryResponseDto?>> GetCategoryByIdAsync(int id)
        {
            var category = await context.Categories
                .AsNoTracking()
                .Select(c => mapper.Map<CategoryResponseDto>(c))
                .FirstOrDefaultAsync(c => c.Id == id);

            return ServiceResult<CategoryResponseDto?>.Ok(category);
        }

        public async Task<ServiceResult<CategoryResponseDto>> CreateCategoryAsync(CategoryDto dto)
        {
            if (dto == null)
                return ServiceResult<CategoryResponseDto>.Fail("Invalid category data", ServiceErrorType.BadRequest);

            bool nameExists = await context.Categories.AnyAsync(c => c.Name == dto.Name);

            if (nameExists)
                return ServiceResult<CategoryResponseDto>.Fail("Category with this name already exists", ServiceErrorType.Conflict);

            if (dto.ParentCategoryId.HasValue)
            {
                bool parentExists = await context.Categories
                    .AnyAsync(c => c.Id == dto.ParentCategoryId.Value);

                if (!parentExists)
                {
                    return ServiceResult<CategoryResponseDto>.Fail("Parent category not found", ServiceErrorType.NotFound);
                }
            }

            var category = mapper.Map<Category>(dto);

            await context.Categories.AddAsync(category);

            await context.SaveChangesAsync();

            return ServiceResult<CategoryResponseDto>.Ok(mapper.Map<CategoryResponseDto>(category));
        }

        public async Task<ServiceResult<CategoryResponseDto>> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            if (dto == null)
                return ServiceResult<CategoryResponseDto>.Fail("Invalid category data", ServiceErrorType.BadRequest);

            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return ServiceResult<CategoryResponseDto>.Fail("Category not found", ServiceErrorType.NotFound);

            bool nameExists = await context.Categories
            .AnyAsync(c => c.Name == dto.Name && c.Id != id);

            if (nameExists)
                return ServiceResult<CategoryResponseDto>.Fail("Category with this name already exists", ServiceErrorType.Conflict);

            if (dto.ParentCategoryId.HasValue)
            {
                if (dto.ParentCategoryId.Value == id)
                    return ServiceResult<CategoryResponseDto>.Fail("Invalid parent category", ServiceErrorType.BadRequest);

                bool parentExists = await context.Categories
                    .AnyAsync(c => c.Id == dto.ParentCategoryId.Value);

                if (!parentExists)
                    return ServiceResult<CategoryResponseDto>.Fail("Parent category not found", ServiceErrorType.NotFound);
            }

            mapper.Map(dto, category);
            await context.SaveChangesAsync();

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