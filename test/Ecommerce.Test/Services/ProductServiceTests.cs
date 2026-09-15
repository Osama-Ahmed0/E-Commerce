using AutoMapper;
using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Mapping;
using ECommerce.Services;
using ECommerce.Services.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECommerce.Tests.Services
{
    public class ProductServiceTests
    {
        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, new LoggerFactory());

            return config.CreateMapper();
        }

        private static Category CreateCategory(int id, string name)
        {
            return new Category
            {
                Id = id,
                Name = name
            };
        }

        private static Product CreateProduct(
            int id,
            string name,
            decimal price,
            int stock,
            int categoryId,
            string description = "")
        {
            return new Product
            {
                Id = id,
                Name = name,
                Description = description,
                Price = price,
                Stock = stock,
                CategoryId = categoryId
            };
        }

        // ============================================================
        // GetProductsAsync
        // ============================================================

        [Fact]
        public async Task GetProductsAsync_productsExist_returnsAllProducts()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");
            context.Categories.Add(category);

            context.Products.AddRange(
                CreateProduct(1, "Laptop", 1000m, 10, 1),
                CreateProduct(2, "Phone", 500m, 20, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, null, null, null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetProductsAsync_categoryIdProvided_returnsOnlyProductsInCategory()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.AddRange(
                CreateCategory(1, "Electronics"),
                CreateCategory(2, "Clothing")
            );

            context.Products.AddRange(
                CreateProduct(1, "Laptop", 1000m, 10, 1),
                CreateProduct(2, "Phone", 500m, 20, 1),
                CreateProduct(3, "Shirt", 50m, 30, 2)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                1, null, null, null, null, null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, p => Assert.Equal(1, p.CategoryId));
        }

        [Fact]
        public async Task GetProductsAsync_minPriceProvided_returnsProductsAboveOrEqualToMinPrice()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Phone", 500m, 10, 1),
                CreateProduct(2, "Laptop", 1000m, 10, 1),
                CreateProduct(3, "Tablet", 750m, 10, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, 750m, null, null, null, null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, p => Assert.True(p.Price >= 750m));
        }

        [Fact]
        public async Task GetProductsAsync_maxPriceProvided_returnsProductsBelowOrEqualToMaxPrice()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Phone", 500m, 10, 1),
                CreateProduct(2, "Laptop", 1000m, 10, 1),
                CreateProduct(3, "Tablet", 750m, 10, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, 750m, null, null, null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, p => Assert.True(p.Price <= 750m));
        }

        [Fact]
        public async Task GetProductsAsync_searchProvided_returnsMatchingProducts()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Gaming Laptop", 1500m, 10, 1, "Powerful gaming computer"),

                CreateProduct(2, "Phone", 500m, 20, 1, "Smart phone"),

                CreateProduct(3, "Keyboard", 100m, 30, 1, "Gaming keyboard")
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, "gaming", null, null, null);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, p => p.Name == "Gaming Laptop");
            Assert.Contains(result.Items, p => p.Name == "Keyboard");
        }

        [Theory]
        [InlineData("price_asc")]
        [InlineData("+price")]
        public async Task GetProductsAsync_priceAscending_returnsProductsSortedAscending(
            string sort)
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Laptop", 1000m, 10, 1),
                CreateProduct(2, "Phone", 500m, 20, 1),
                CreateProduct(3, "Tablet", 750m, 30, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, null, sort, null, null);

            // Assert
            Assert.Equal(500m, result.Items[0].Price);
            Assert.Equal(750m, result.Items[1].Price);
            Assert.Equal(1000m, result.Items[2].Price);
        }

        [Theory]
        [InlineData("price_desc")]
        [InlineData("-price")]
        public async Task GetProductsAsync_priceDescending_returnsProductsSortedDescending(
            string sort)
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Laptop", 1000m, 10, 1),
                CreateProduct(2, "Phone", 500m, 20, 1),
                CreateProduct(3, "Tablet", 750m, 30, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, null, sort, null, null);

            // Assert
            Assert.Equal(1000m, result.Items[0].Price);
            Assert.Equal(750m, result.Items[1].Price);
            Assert.Equal(500m, result.Items[2].Price);
        }

        [Fact]
        public async Task GetProductsAsync_paginationProvided_returnsCorrectPage()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Categories.Add(CreateCategory(1, "Electronics"));

            context.Products.AddRange(
                CreateProduct(1, "Product 1", 100m, 10, 1),
                CreateProduct(2, "Product 2", 200m, 10, 1),
                CreateProduct(3, "Product 3", 300m, 10, 1),
                CreateProduct(4, "Product 4", 400m, 10, 1),
                CreateProduct(5, "Product 5", 500m, 10, 1)
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();
            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, null, "price_asc", 2, 2);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(3, result.TotalPages);

            Assert.Equal(300m, result.Items[0].Price);
            Assert.Equal(400m, result.Items[1].Price);
        }

        [Fact]
        public async Task GetProductsAsync_noProducts_returnsEmptyResult()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductsAsync(
                null, null, null, null, null, null, null);

            // Assert
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, result.TotalPages);
        }

        // ============================================================
        // GetProductByIdAsync
        // ============================================================

        [Fact]
        public async Task GetProductByIdAsync_existingProduct_returnsProduct()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");

            var product = CreateProduct(
                1,
                "Laptop",
                1000m,
                10,
                1,
                "Gaming laptop");

            product.Category = category;

            context.Categories.Add(category);
            context.Products.Add(product);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductByIdAsync(1);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.Id);
            Assert.Equal("Laptop", result.Data.Name);
            Assert.Equal(1000m, result.Data.Price);
            Assert.Equal("Electronics", result.Data.CategoryName);
        }

        [Fact]
        public async Task GetProductByIdAsync_productDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.GetProductByIdAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Product not found", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        // ============================================================
        // CreateProductAsync
        // ============================================================

        [Fact]
        public async Task CreateProductAsync_validProduct_returnsCreatedProduct()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");
            context.Categories.Add(category);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new ProductDto
            {
                Name = "Laptop",
                Description = "Gaming laptop",
                Price = 1500m,
                Stock = 10,
                CategoryId = 1
            };

            var mapper = CreateMapper();

            var validator = new Mock<IProductValidator>();
            validator
                .Setup(v => v.ValidateForCreateAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.CreateProductAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Laptop", result.Data.Name);
            Assert.Equal(1500m, result.Data.Price);
            Assert.Equal(10, result.Data.Stock);
            Assert.Equal(1, result.Data.CategoryId);
            Assert.Equal("Electronics", result.Data.CategoryName);

            var productInDb = await context.Products.FirstOrDefaultAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(productInDb);
            Assert.Equal("Laptop", productInDb.Name);

            validator.Verify(
                v => v.ValidateForCreateAsync(dto),
                Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new ProductDto
            {
                Name = "Laptop",
                Price = 1500m,
                Stock = 10,
                CategoryId = 1
            };

            var mapper = CreateMapper();

            var validator = new Mock<IProductValidator>();

            validator
                .Setup(v => v.ValidateForCreateAsync(dto))
                .ReturnsAsync(
                    ValidationResult.Invalid(
                        "Product name already exists",
                        ServiceErrorType.Conflict));

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.CreateProductAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Product name already exists", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);

            Assert.Empty(context.Products);

            validator.Verify(
                v => v.ValidateForCreateAsync(dto),
                Times.Once);
        }

        // ============================================================
        // UpdateProductAsync
        // ============================================================

        [Fact]
        public async Task UpdateProductAsync_existingProduct_validDto_updatesProduct()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");

            var product = CreateProduct(1, "Old Laptop", 1000m, 5, 1);

            product.Category = category;

            context.Categories.Add(category);
            context.Products.Add(product);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new ProductDto
            {
                Id = 1,
                Name = "New Laptop",
                Description = "Updated description",
                Price = 1500m,
                Stock = 20,
                CategoryId = 1
            };

            var mapper = CreateMapper();

            var validator = new Mock<IProductValidator>();

            validator.Setup(v => v.ValidateForUpdateAsync(dto, It.IsAny<Product>()))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.UpdateProductAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal("New Laptop", result.Data.Name);
            Assert.Equal("Updated description", result.Data.Description);
            Assert.Equal(1500m, result.Data.Price);
            Assert.Equal(20, result.Data.Stock);

            var productInDb = await context.Products.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.NotNull(productInDb);
            Assert.Equal("New Laptop", productInDb.Name);
            Assert.Equal(1500m, productInDb.Price);
            Assert.Equal(20, productInDb.Stock);

            validator.Verify(
                v => v.ValidateForUpdateAsync(dto, It.IsAny<Product>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_productDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new ProductDto
            {
                Id = 999,
                Name = "Laptop",
                Price = 1000m,
                Stock = 10,
                CategoryId = 1
            };

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.UpdateProductAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Product not found", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);

            validator.Verify(
                v => v.ValidateForUpdateAsync(
                    It.IsAny<ProductDto>(),
                    It.IsAny<Product>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateProductAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");

            var product = CreateProduct(1, "Laptop", 1000m, 10, 1);

            product.Category = category;

            context.Categories.Add(category);
            context.Products.Add(product);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new ProductDto
            {
                Id = 1,
                Name = "Laptop",
                Price = 500m,
                Stock = 10,
                CategoryId = 1
            };

            var mapper = CreateMapper();

            var validator = new Mock<IProductValidator>();

            validator
                .Setup(v => v.ValidateForUpdateAsync(dto, It.IsAny<Product>()))
                .ReturnsAsync(
                    ValidationResult.Invalid(
                        "Product name already exists",
                        ServiceErrorType.Conflict));

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.UpdateProductAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Product name already exists", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);

            var productInDb = await context.Products.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.NotNull(productInDb);
            Assert.Equal("Laptop", productInDb.Name);
            Assert.Equal(1000m, productInDb.Price);
        }

        // ============================================================
        // DeleteProductAsync
        // ============================================================

        [Fact]
        public async Task DeleteProductAsync_existingProduct_deletesProduct()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var category = CreateCategory(1, "Electronics");

            context.Categories.Add(category);

            context.Products.Add(
                CreateProduct(1, "Laptop", 1000m, 10, 1));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.DeleteProductAsync(1);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);

            var productInDb = await context.Products.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);

            Assert.Null(productInDb);
        }

        [Fact]
        public async Task DeleteProductAsync_productDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<IProductValidator>();

            var service = new ProductService(context, mapper, validator.Object);

            // Act
            var result = await service.DeleteProductAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.Data);
            Assert.Equal("Product not found", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }
    }

}