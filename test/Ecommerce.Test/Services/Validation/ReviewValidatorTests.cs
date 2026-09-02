using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;

namespace Ecommerce.Test.Services.Validation
{
    public class ReviewValidatorTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(999)]
        public async Task ValidateForCreateAsync_productDoesNotExist_returnsNotFoundError(
            int productId)
        {
            // Arrange
            var context = new InMemoryDbContext();
            var validator = new ReviewValidator(context);

            var userId = "user-1";

            var dto = new WriteReviewDto
            {
                // Add the required properties of your DTO here if needed
            };

            // Act
            var result = await validator.ValidateForCreateAsync(
                productId,
                userId,
                dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Product not found",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        [Theory]
        [InlineData("user-1")]
        [InlineData("user-2")]
        [InlineData("user-100")]
        public async Task ValidateForCreateAsync_productExistsAndUserHasNotReviewed_returnsValidResult(
            string userId)
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

            var validator = new ReviewValidator(context);

            var dto = new WriteReviewDto
            {
                // Add the required properties of your DTO here if needed
            };

            // Act
            var result = await validator.ValidateForCreateAsync(
                1,
                userId,
                dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData("user-1")]
        [InlineData("user-2")]
        [InlineData("user-100")]
        public async Task ValidateForCreateAsync_userAlreadyReviewedProduct_returnsConflictError(
            string userId)
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            context.Reviews.Add(new Review
            {
                ProductId = 1,
                UserId = userId
            });

            await context.SaveChangesAsync();

            var validator = new ReviewValidator(context);

            var dto = new WriteReviewDto
            {
                // Add the required properties of your DTO here if needed
            };

            // Act
            var result = await validator.ValidateForCreateAsync(
                1,
                userId,
                dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "User has already reviewed this product",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.Conflict,
                result.ErrorType);
        }

        [Fact]
        public async Task ValidateForCreateAsync_differentUserAlreadyReviewedProduct_returnsValidResult()
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Laptop",
                Stock = 10
            });

            context.Reviews.Add(new Review
            {
                ProductId = 1,
                UserId = "user-1"
            });

            await context.SaveChangesAsync();

            var validator = new ReviewValidator(context);

            var dto = new WriteReviewDto
            {
                // Add the required properties of your DTO here if needed
            };

            // Act
            var result = await validator.ValidateForCreateAsync(
                1,
                "user-2",
                dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateForCreateAsync_userReviewedDifferentProduct_returnsValidResult()
        {
            // Arrange
            var context = new InMemoryDbContext();

            context.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Name = "Laptop",
                    Stock = 10
                },
                new Product
                {
                    Id = 2,
                    Name = "Phone",
                    Stock = 20
                });

            context.Reviews.Add(new Review
            {
                ProductId = 2,
                UserId = "user-1"
            });

            await context.SaveChangesAsync();

            var validator = new ReviewValidator(context);

            var dto = new WriteReviewDto
            {
                // Add the required properties of your DTO here if needed
            };

            // Act
            var result = await validator.ValidateForCreateAsync(
                1,
                "user-1",
                dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }
}