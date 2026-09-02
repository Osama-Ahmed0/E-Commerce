using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;

namespace Ecommerce.Test.Services.Validation
{
    public class CartValidatorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task ValidateForAddAsync_invalidQuantity_returnsValidationError(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new CartValidator(context);

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = quantity
            };

            // Act
            var result = await validator.ValidateForAddAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Quantity must be greater than zero.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public async Task ValidateForAddAsync_productDoesNotExist_returnsNotFoundError(int productId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new CartValidator(context);

            var dto = new CartItemDto
            {
                ProductId = productId,
                Quantity = 1
            };

            // Act
            var result = await validator.ValidateForAddAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal($"Product {productId} not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(15)]
        [InlineData(100)]
        public async Task ValidateForAddAsync_quantityExceedsStock_returnsValidationError(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            await context.SaveChangesAsync();

            var validator = new CartValidator(context);

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = quantity
            };

            // Act
            var result = await validator.ValidateForAddAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Quantity exceeds available stock for product 1.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ValidateForAddAsync_quantityWithinStock_returnsValidResult(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            await context.SaveChangesAsync();

            var validator = new CartValidator(context);

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = quantity
            };

            // Act
            var result = await validator.ValidateForAddAsync(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task ValidateForUpdateAsync_invalidQuantity_returnsValidationError(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new CartValidator(context);

            var productId = 1;

            // Act
            var result = await validator.ValidateForUpdateAsync(
                productId,
                quantity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Quantity must be greater than zero.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public async Task ValidateForUpdateAsync_productDoesNotExist_returnsNotFoundError(int productId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new CartValidator(context);

            var quantity = 1;

            // Act
            var result = await validator.ValidateForUpdateAsync(
                productId,
                quantity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal($"Product {productId} not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(15)]
        [InlineData(100)]
        public async Task ValidateForUpdateAsync_quantityExceedsStock_returnsValidationError(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            await context.SaveChangesAsync();

            var validator = new CartValidator(context);

            var productId = 1;

            // Act
            var result = await validator.ValidateForUpdateAsync(
                productId,
                quantity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Quantity exceeds available stock for product 1.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ValidateForUpdateAsync_quantityWithinStock_returnsValidResult(int quantity)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            await context.SaveChangesAsync();

            var validator = new CartValidator(context);

            var productId = 1;

            // Act
            var result = await validator.ValidateForUpdateAsync(
                productId,
                quantity);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }
}