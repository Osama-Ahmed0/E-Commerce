using AutoMapper;
using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Mapping;
using ECommerce.Services;
using ECommerce.Services.Validation;
using ECommerce.Tests;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryValidator> validatorMock;

        public CategoryServiceTests()
        {
            validatorMock = new Mock<ICategoryValidator>();
        }

        private CategoryService CreateService(InMemoryDbContext context)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, new LoggerFactory());

            var mapper = config.CreateMapper();

            return new CategoryService(
                context,
                mapper,
                validatorMock.Object);
        }

        // ---------------------------------------------------------
        // GetCategoriesAsync
        // ---------------------------------------------------------

        [Fact]
        public async Task GetCategoriesAsync_emptyDatabase_resultEmpty()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoriesAsync(1, 10);

            // Assert
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(0, result.TotalPages);
        }

        [Fact]
        public async Task GetCategoriesAsync_categoriesExist_returnsPagedCategories()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.AddRange(
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" },
                new Category { Name = "Books" }
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoriesAsync(1, 2);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(2, result.TotalPages);
        }

        [Fact]
        public async Task GetCategoriesAsync_secondPage_returnsRemainingCategories()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.AddRange(
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" },
                new Category { Name = "Books" }
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoriesAsync(2, 2);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(2, result.TotalPages);
        }

        [Fact]
        public async Task GetCategoriesAsync_nullPagination_usesDefaultPagination()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.AddRange(
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" }
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoriesAsync(null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);

            // PageSize depends on your PaginationHelper default.
            // Assert.Equal(expectedDefaultPageSize, result.PageSize);
        }

        // ---------------------------------------------------------
        // GetCategoryByIdAsync
        // ---------------------------------------------------------

        [Fact]
        public async Task GetCategoryByIdAsync_existingId_returnsCategory()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = new Category
            {
                Name = "Electronics"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoryByIdAsync(category.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(category.Id, result.Data.Id);
            Assert.Equal("Electronics", result.Data.Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_nonExistingId_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var service = CreateService(context);

            // Act
            var result = await service.GetCategoryByIdAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category not found", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        // ---------------------------------------------------------
        // CreateCategoryAsync
        // ---------------------------------------------------------

        [Fact]
        public async Task CreateCategoryAsync_validCategory_createsCategory()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new CategoryDto
            {
                Name = "Electronics"
            };

            validatorMock
                .Setup(v => v.ValidateForCreateAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = CreateService(context);

            // Act
            var result = await service.CreateCategoryAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Electronics", result.Data.Name);

            var category = await context.Categories.FindAsync([result.Data.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.NotNull(category);
            Assert.Equal("Electronics", category.Name);

            validatorMock.Verify(
                v => v.ValidateForCreateAsync(dto),
                Times.Once);
        }

        [Fact]
        public async Task CreateCategoryAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new CategoryDto
            {
                Name = "Electronics"
            };

            validatorMock
                .Setup(v => v.ValidateForCreateAsync(dto))
                .ReturnsAsync(ValidationResult.Invalid("Category name already exists", ServiceErrorType.Conflict));

            var service = CreateService(context);

            // Act
            var result = await service.CreateCategoryAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(
                "Category name already exists",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.Conflict,
                result.ErrorType);

            Assert.Empty(context.Categories);

            validatorMock.Verify(
                v => v.ValidateForCreateAsync(dto),
                Times.Once);
        }

        [Fact]
        public async Task CreateCategoryAsync_withParent_createsCategoryWithParent()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var parent = new Category
            {
                Name = "Electronics"
            };

            context.Categories.Add(parent);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CategoryDto
            {
                Name = "Laptops",
                ParentCategoryId = parent.Id
            };

            validatorMock
                .Setup(v => v.ValidateForCreateAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = CreateService(context);

            // Act
            var result = await service.CreateCategoryAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal("Laptops", result.Data.Name);
            Assert.Equal(parent.Id, result.Data.ParentCategoryId);
            Assert.Equal("Electronics", result.Data.ParentCategoryName);
        }

        // ---------------------------------------------------------
        // UpdateCategoryAsync
        // ---------------------------------------------------------

        [Fact]
        public async Task UpdateCategoryAsync_existingCategory_updatesCategory()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = new Category
            {
                Name = "Electronics"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = "Electronic Devices"
            };

            validatorMock
                .Setup(v => v.ValidateForUpdateAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = CreateService(context);

            // Act
            var result = await service.UpdateCategoryAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Electronic Devices", result.Data.Name);

            var updatedCategory = await context.Categories.FindAsync([category.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.NotNull(updatedCategory);
            Assert.Equal("Electronic Devices", updatedCategory.Name);

            validatorMock.Verify(v => v.ValidateForUpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryAsync_nonExistingId_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new CategoryDto
            {
                Id = 999,
                Name = "Electronics"
            };

            var service = CreateService(context);

            // Act
            var result = await service.UpdateCategoryAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(
                "Category not found",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);

            validatorMock.Verify(v => v.ValidateForUpdateAsync(It.IsAny<CategoryDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCategoryAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = new Category
            {
                Name = "Electronics"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = "Existing Name"
            };

            validatorMock
                .Setup(v => v.ValidateForUpdateAsync(dto))
                .ReturnsAsync(
                    ValidationResult.Invalid("Category name already exists", ServiceErrorType.Conflict));

            var service = CreateService(context);

            // Act
            var result = await service.UpdateCategoryAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category name already exists", result.ErrorMessage);

            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }

        [Fact]
        public async Task UpdateCategoryAsync_withParent_updatesParent()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var parent = new Category
            {
                Name = "Electronics"
            };

            var category = new Category
            {
                Name = "Old Name"
            };

            context.Categories.AddRange(parent, category);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = "Laptops",
                ParentCategoryId = parent.Id
            };

            validatorMock
                .Setup(v => v.ValidateForUpdateAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = CreateService(context);

            // Act
            var result = await service.UpdateCategoryAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal("Laptops", result.Data.Name);
            Assert.Equal(parent.Id, result.Data.ParentCategoryId);
            Assert.Equal("Electronics", result.Data.ParentCategoryName);
        }

        // ---------------------------------------------------------
        // DeleteCategoryAsync
        // ---------------------------------------------------------

        [Fact]
        public async Task DeleteCategoryAsync_existingCategory_deletesCategory()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = new Category
            {
                Name = "Electronics"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.DeleteCategoryAsync(category.Id);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);

            var deletedCategory =
                await context.Categories.FindAsync([category.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.Null(deletedCategory);
        }

        [Fact]
        public async Task DeleteCategoryAsync_nonExistingId_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var service = CreateService(context);

            // Act
            var result = await service.DeleteCategoryAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(
                "Category not found",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        [Fact]
        public async Task DeleteCategoryAsync_categoryHasChildren_returnsBadRequest()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var parent = new Category
            {
                Name = "Electronics"
            };

            await context.Categories.AddAsync(parent, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var child = new Category
            {
                Name = "Laptops",
                ParentCategoryId = parent.Id
            };

            await context.Categories.AddAsync(child, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.DeleteCategoryAsync(parent.Id);

            // Assert
            Assert.False(result.Success);

            Assert.Equal(
                "Cannot delete category with child nodes",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.BadRequest,
                result.ErrorType);

            var category =
                await context.Categories.FindAsync([parent.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.NotNull(category);
        }
    }
}