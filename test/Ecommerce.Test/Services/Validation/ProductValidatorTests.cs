using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;

namespace Ecommerce.Test.Services.Validation
{
    public class ProductValidatorTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task ValidateForCreateAsync_existingCategory_returnsValidResult(
            int categoryId)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Categories.Add(new Category
            {
                Id = categoryId
            });

            await context.SaveChangesAsync();

            var validator = new ProductValidator(context);

            var dto = new ProductDto
            {
                CategoryId = categoryId
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(999)]
        public async Task ValidateForCreateAsync_nonExistingCategory_returnsNotFoundError(
            int categoryId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new ProductValidator(context);

            var dto = new ProductDto
            {
                CategoryId = categoryId
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Category not found",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task ValidateForUpdateAsync_categoryIsUnchanged_returnsValidResult(
            int categoryId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new ProductValidator(context);

            var dto = new ProductDto
            {
                CategoryId = categoryId
            };

            var existingProduct = new Product
            {
                Id = 1,
                Name = "Laptop",
                CategoryId = categoryId
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(
                dto,
                existingProduct);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task ValidateForUpdateAsync_categoryChangedToExistingCategory_returnsValidResult(
            int newCategoryId)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Categories.Add(new Category
            {
                Id = newCategoryId
            });

            await context.SaveChangesAsync();

            var validator = new ProductValidator(context);

            var dto = new ProductDto
            {
                CategoryId = newCategoryId
            };

            var existingProduct = new Product
            {
                Id = 1,
                Name = "Laptop",
                CategoryId = 99
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(
                dto,
                existingProduct);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(999)]
        public async Task ValidateForUpdateAsync_categoryChangedToNonExistingCategory_returnsNotFoundError(
            int newCategoryId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new ProductValidator(context);

            var dto = new ProductDto
            {
                CategoryId = newCategoryId
            };

            var existingProduct = new Product
            {
                Id = 1,
                Name = "Laptop",
                CategoryId = 10
            };

            // Act
            var result = await validator.ValidateForUpdateAsync(
                dto,
                existingProduct);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Category not found",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }
    }
}