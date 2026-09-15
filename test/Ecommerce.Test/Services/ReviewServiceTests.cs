using AutoMapper;
using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Mapping;
using ECommerce.Services;
using ECommerce.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Services;

public class ReviewServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfiles>();
        }, new LoggerFactory());

        return config.CreateMapper();
    }

    private static User CreateUser(
        string id,
        string userName)
    {
        return new User
        {
            Id = id,
            UserName = userName
        };
    }

    private static Product CreateProduct(
        int id,
        string name = "Laptop")
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = "Test product",
            Price = 1000m,
            Stock = 10,
            CategoryId = 1
        };
    }

    // ============================================================
    // GetReviewsAsync
    // ============================================================

    [Fact]
    public async Task GetReviewsAsync_productHasReviews_returnsReviews()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user1 = CreateUser("user-1", "John");
        var user2 = CreateUser("user-2", "Sarah");

        var product = CreateProduct(1);

        context.Users.AddRange(user1, user2);
        context.Products.Add(product);

        context.Reviews.AddRange(
            new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Excellent product",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 2,
                Rating = 4,
                Comment = "Very good",
                CreatedAt = DateTime.UtcNow,
                ProductId = 1,
                UserId = "user-2"
            });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mapper = CreateMapper();
        var validator = new Mock<IReviewValidator>();

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.GetReviewsAsync(
            1,
            null,
            null);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);

        Assert.Equal(5, result.Items[1].Rating);
        Assert.Equal(4, result.Items[0].Rating);

        Assert.Equal("John", result.Items[1].UserName);
        Assert.Equal("Sarah", result.Items[0].UserName);
    }

    [Fact]
    public async Task GetReviewsAsync_productHasNoReviews_returnsEmptyResult()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var mapper = CreateMapper();
        var validator = new Mock<IReviewValidator>();

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.GetReviewsAsync(
            1,
            null,
            null);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetReviewsAsync_reviewsForDifferentProduct_returnsOnlyRequestedProductReviews()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user1 = CreateUser("user-1", "John");
        var user2 = CreateUser("user-2", "Sarah");

        var product1 = CreateProduct(1, "Laptop");
        var product2 = CreateProduct(2, "Phone");

        context.Users.AddRange(user1, user2);
        context.Products.AddRange(product1, product2);

        context.Reviews.AddRange(
            new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Laptop review",
                CreatedAt = DateTime.UtcNow,
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 2,
                Rating = 3,
                Comment = "Phone review",
                CreatedAt = DateTime.UtcNow,
                ProductId = 2,
                UserId = "user-2"
            });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mapper = CreateMapper();
        var validator = new Mock<IReviewValidator>();

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.GetReviewsAsync(
            1,
            null,
            null);

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);

        Assert.Equal(
            "Laptop review",
            item.Comment);

        Assert.Equal(
            5,
            item.Rating);
    }

    [Fact]
    public async Task GetReviewsAsync_reviewsExist_returnsNewestReviewsFirst()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user = CreateUser("user-1", "John");
        var product = CreateProduct(1);

        context.Users.Add(user);
        context.Products.Add(product);

        context.Reviews.AddRange(
            new Review
            {
                Id = 1,
                Rating = 2,
                Comment = "Old review",
                CreatedAt = new DateTime(2025, 1, 1),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 2,
                Rating = 5,
                Comment = "New review",
                CreatedAt = new DateTime(2025, 2, 1),
                ProductId = 1,
                UserId = "user-1"
            });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mapper = CreateMapper();
        var validator = new Mock<IReviewValidator>();

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.GetReviewsAsync(
            1,
            null,
            null);

        // Assert
        Assert.Equal(2, result.Items.Count);

        Assert.Equal(
            "New review",
            result.Items[0].Comment);

        Assert.Equal(
            "Old review",
            result.Items[1].Comment);
    }

    [Fact]
    public async Task GetReviewsAsync_paginationProvided_returnsCorrectPage()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user = CreateUser("user-1", "John");
        var product = CreateProduct(1);

        context.Users.Add(user);
        context.Products.Add(product);

        context.Reviews.AddRange(
            new Review
            {
                Id = 1,
                Rating = 1,
                Comment = "Review 1",
                CreatedAt = new DateTime(2025, 1, 1),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 2,
                Rating = 2,
                Comment = "Review 2",
                CreatedAt = new DateTime(2025, 1, 2),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 3,
                Rating = 3,
                Comment = "Review 3",
                CreatedAt = new DateTime(2025, 1, 3),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 4,
                Rating = 4,
                Comment = "Review 4",
                CreatedAt = new DateTime(2025, 1, 4),
                ProductId = 1,
                UserId = "user-1"
            },
            new Review
            {
                Id = 5,
                Rating = 5,
                Comment = "Review 5",
                CreatedAt = new DateTime(2025, 1, 5),
                ProductId = 1,
                UserId = "user-1"
            });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mapper = CreateMapper();
        var validator = new Mock<IReviewValidator>();

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.GetReviewsAsync(
            1,
            2,
            2);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);

        Assert.Equal(
            "Review 3",
            result.Items[0].Comment);

        Assert.Equal(
            "Review 2",
            result.Items[1].Comment);
    }

    // ============================================================
    // CreateReviewAsync
    // ============================================================

    [Fact]
    public async Task CreateReviewAsync_validReview_createsAndReturnsReview()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user = CreateUser(
            "user-1",
            "John");

        context.Users.Add(user);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new WriteReviewDto
        {
            Rating = 5,
            Comment = "Excellent product!"
        };

        var mapper = CreateMapper();

        var validator = new Mock<IReviewValidator>();

        validator
            .Setup(v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto))
            .ReturnsAsync(ValidationResult.Valid());

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.CreateReviewAsync(
            1,
            "user-1",
            dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        Assert.Equal(5, result.Data.Rating);
        Assert.Equal(
            "Excellent product!",
            result.Data.Comment);

        Assert.Equal(
            "John",
            result.Data.UserName);

        Assert.NotEqual(
            default,
            result.Data.CreatedAt);

        var reviewInDb = await context.Reviews.FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(reviewInDb);

        Assert.Equal(5, reviewInDb.Rating);
        Assert.Equal(
            "Excellent product!",
            reviewInDb.Comment);

        Assert.Equal(
            1,
            reviewInDb.ProductId);

        Assert.Equal(
            "user-1",
            reviewInDb.UserId);

        validator.Verify(
            v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto),
            Times.Once);
    }

    [Fact]
    public async Task CreateReviewAsync_nullComment_createsReviewWithEmptyComment()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user = CreateUser(
            "user-1",
            "John");

        context.Users.Add(user);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new WriteReviewDto
        {
            Rating = 4,
            Comment = null
        };

        var mapper = CreateMapper();

        var validator = new Mock<IReviewValidator>();

        validator
            .Setup(v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto))
            .ReturnsAsync(ValidationResult.Valid());

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.CreateReviewAsync(
            1,
            "user-1",
            dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        Assert.Equal(
            string.Empty,
            result.Data.Comment);

        var reviewInDb = await context.Reviews.FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(reviewInDb);
        Assert.Equal(
            string.Empty,
            reviewInDb.Comment);
    }

    [Fact]
    public async Task CreateReviewAsync_validationFails_returnsValidationError()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var dto = new WriteReviewDto
        {
            Rating = 6,
            Comment = "Invalid rating"
        };

        var mapper = CreateMapper();

        var validator = new Mock<IReviewValidator>();

        validator
            .Setup(v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto))
            .ReturnsAsync(
                ValidationResult.Invalid(
                    "Rating must be between 1 and 5",
                    ServiceErrorType.Validation));

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.CreateReviewAsync(
            1,
            "user-1",
            dto);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);

        Assert.Equal(
            "Rating must be between 1 and 5",
            result.ErrorMessage);

        Assert.Equal(
            ServiceErrorType.Validation,
            result.ErrorType);

        Assert.Empty(context.Reviews);

        validator.Verify(
            v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto),
            Times.Once);
    }

    [Fact]
    public async Task CreateReviewAsync_userDoesNotExist_returnsNotFound()
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var dto = new WriteReviewDto
        {
            Rating = 5,
            Comment = "Great product"
        };

        var mapper = CreateMapper();

        var validator = new Mock<IReviewValidator>();

        validator
            .Setup(v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto))
            .ReturnsAsync(ValidationResult.Valid());

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.CreateReviewAsync(
            1,
            "user-1",
            dto);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);

        Assert.Equal(
            "User not found",
            result.ErrorMessage);

        Assert.Equal(
            ServiceErrorType.NotFound,
            result.ErrorType);

        Assert.Empty(context.Reviews);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task CreateReviewAsync_validRating_createsReview(
        int rating)
    {
        // Arrange
        using var context = new InMemoryDbContext();

        var user = CreateUser(
            "user-1",
            "John");

        context.Users.Add(user);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new WriteReviewDto
        {
            Rating = rating,
            Comment = "Test review"
        };

        var mapper = CreateMapper();

        var validator = new Mock<IReviewValidator>();

        validator
            .Setup(v => v.ValidateForCreateAsync(
                1,
                "user-1",
                dto))
            .ReturnsAsync(ValidationResult.Valid());

        var service = new ReviewService(
            context,
            mapper,
            validator.Object);

        // Act
        var result = await service.CreateReviewAsync(
            1,
            "user-1",
            dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(rating, result.Data.Rating);
    }
}