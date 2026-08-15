using ECommerce.Data;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    public class CategoryService(AppDbContext context) : ICategoryService
    {
        public async Task<IEnumerable<CategoryResponseDto>> GetCategoriesAsync()
        {
            var categories = await context.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory.Name
            })
            .ToListAsync();
            return categories;
        }
        public async Task<CategoryResponseDto?> GetCategoryByIdAsync(int id)
        {
            var category = await context.Categories
                .AsNoTracking()
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory.Name
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            return category;
        }

        public async Task<bool> CreateCategoryAsync(CategoryDto dto)
        {
            if (dto == null) return false;


            bool nameExists = await context.Categories.AnyAsync(c => c.Name == dto.Name);

            if (nameExists)
                return false;

            if (dto.ParentCategoryId.HasValue)
            {
                bool parentExists = await context.Categories
                    .AnyAsync(c => c.Id == dto.ParentCategoryId.Value);

                if (!parentExists)
                {
                    return false;
                }
            }

            var category = new Data.Models.Category
            {
                Name = dto.Name,
                ParentCategoryId = dto.ParentCategoryId
            };

            await context.Categories.AddAsync(category);

            await context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            if (dto == null) return false;

            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return false;

            bool nameExists = await context.Categories
            .AnyAsync(c => c.Name == dto.Name && c.Id != id);

            if (nameExists) return false;

            if (dto.ParentCategoryId.HasValue)
            {
                if (dto.ParentCategoryId.Value == id)
                    return false;

                bool parentExists = await context.Categories
                    .AnyAsync(c => c.Id == dto.ParentCategoryId.Value);

                if (!parentExists)
                    return false;
            }

            category.Name = dto.Name;
            category.ParentCategoryId = dto.ParentCategoryId;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return false;

            var hasChild = await context.Categories.AnyAsync(c => c.ParentCategoryId == id);
            if (hasChild)
                return false;

            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return true;
        }
    }
}