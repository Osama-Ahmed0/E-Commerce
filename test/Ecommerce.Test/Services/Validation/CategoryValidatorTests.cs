using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;

namespace Ecommerce.Test.Services.Validation
{
    public class CategoryValidatorTests
    {
        [Fact]
        public async Task ValidateForCreateAsync_uniqueName_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Electronics",
                ParentCategoryId = null
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.True(result.IsValid);
        }


        [Fact]
        public async Task ValidateForCreateAsync_existingNameSameParent_resultConflict()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Phones", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = null
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }


        [Fact]
        public async Task ValidateForCreateAsync_sameNameDifferentParent_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Phones", ParentCategoryId = null });
            context.Categories.Add(new Category { Id = 2, Name = "Electronics", ParentCategoryId = null });

            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = 2
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.True(result.IsValid);
        }


        [Fact]
        public async Task ValidateForCreateAsync_nonExistingParent_resultNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = 999
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }


        [Fact]
        public async Task ValidateForCreateAsync_existingParent_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            context.SaveChanges();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = 1
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.True(result.IsValid);
        }




        [Fact]
        public async Task ValidateForUpdateAsync_sameCategoryName_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Electronics",
                ParentCategoryId = null
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(1, dto);

            // Assert
            Assert.True(result.IsValid);
        }


        [Fact]
        public async Task ValidateForUpdateAsync_nameExistsInAnotherCategory_resultConflict()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            context.Categories.Add(new Category { Id = 2, Name = "Phones", ParentCategoryId = null });

            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = null
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(1, dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }


        [Fact]
        public async Task ValidateForUpdateAsync_parentIsSameCategory_resultBadRequest()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Electronics",
                ParentCategoryId = 1
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(1, dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ServiceErrorType.BadRequest, result.ErrorType);
        }


        [Fact]
        public async Task ValidateForUpdateAsync_nonExistingParent_resultNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = 999
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(1, dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }


        [Fact]
        public async Task ValidateForUpdateAsync_existingParent_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            context.Categories.Add(new Category { Id = 2, Name = "Phones", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Smartphones",
                ParentCategoryId = 1
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(2, dto);

            // Assert
            Assert.True(result.IsValid);
        }


        [Fact]
        public async Task ValidateForUpdateAsync_sameNameDifferentParent_resultValid()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(new Category { Id = 1, Name = "Electronics", ParentCategoryId = null });
            context.Categories.Add(new Category { Id = 2, Name = "Phones", ParentCategoryId = 1 });
            context.Categories.Add(new Category { Id = 3, Name = "Clothes", ParentCategoryId = null });
            await context.SaveChangesAsync();

            var validator = new CategoryValidator(context);

            var dto = new CategoryDto
            {
                Name = "Phones",
                ParentCategoryId = 3
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(2, dto);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}