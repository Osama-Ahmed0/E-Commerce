using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services.Validation
{
    public class CategoryValidator(AppDbContext context) : ICategoryValidator
    {
        private readonly AppDbContext context = context;

        public async Task<ValidationResult> ValidateForCreateAsync(CategoryDto dto)
        {
            if (await NameExistsAsync(dto.Name, dto.ParentCategoryId))
                return ValidationResult.Invalid("Category with this name already exists", ServiceErrorType.Conflict);

            if (dto.ParentCategoryId.HasValue && !await ParentCategoryExistsAsync(dto.ParentCategoryId.Value))
                return ValidationResult.Invalid("Parent category not found", ServiceErrorType.NotFound);

            return ValidationResult.Valid();
        }

        public async Task<ValidationResult> ValidateForUpdateAsync(int id, CategoryDto dto)
        {
            if (await NameExistsAsync(dto.Name, dto.ParentCategoryId, excludeId: id))
                return ValidationResult.Invalid("Category with this name already exists", ServiceErrorType.Conflict);

            if (dto.ParentCategoryId.HasValue)
            {
                if (dto.ParentCategoryId.Value == id)
                    return ValidationResult.Invalid("Invalid parent category", ServiceErrorType.BadRequest);

                if (!await ParentCategoryExistsAsync(dto.ParentCategoryId.Value))
                    return ValidationResult.Invalid("Parent category not found", ServiceErrorType.NotFound);
            }

            return ValidationResult.Valid();
        }

        private async Task<bool> NameExistsAsync(string name, int? parentCategoryId, int? excludeId = null)
        {
            var query = context.Categories.Where(c => c.Name == name && c.ParentCategoryId == parentCategoryId);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        private async Task<bool> ParentCategoryExistsAsync(int parentCategoryId) =>
            await context.Categories.AnyAsync(c => c.Id == parentCategoryId);
    }
}