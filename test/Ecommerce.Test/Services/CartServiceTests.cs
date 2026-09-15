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

namespace ECommerce.Tests.Services
{
    public class CartServiceTests
    {
        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, new LoggerFactory());

            return config.CreateMapper();
        }

        private static Product CreateProduct(
            int id,
            string name = "Laptop",
            decimal price = 1000m)
        {
            return new Product
            {
                Id = id,
                Name = name,
                Description = "Test product",
                Price = price,
                Stock = 10,
                CategoryId = 1
            };
        }

        private static Cart CreateCart(
            string userId,
            params CartItem[] items)
        {
            return new Cart
            {
                UserId = userId,
                CartItems = [.. items]
            };
        }

        // ============================================================
        // GetCartItemsAsync
        // ============================================================

        [Fact]
        public async Task GetCartItemsAsync_existingCart_returnsCartItems()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product = CreateProduct(1);

            var cart = CreateCart(
                "user-1",
                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product
                });

            context.Products.Add(product);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.GetCartItemsAsync("user-1");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data);

            Assert.Equal(1, item.ProductId);
            Assert.Equal(2, item.Quantity);
        }

        [Fact]
        public async Task GetCartItemsAsync_cartDoesNotExist_createsNewCartAndReturnsEmptyList()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.GetCartItemsAsync("user-1");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);

            var cart = await context.Carts
                .FirstOrDefaultAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            Assert.NotNull(cart);
            Assert.Equal("user-1", cart.UserId);
        }

        [Fact]
        public async Task GetCartItemsAsync_existingCartWithMultipleItems_returnsAllItems()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product1 = CreateProduct(1, "Laptop");
            var product2 = CreateProduct(2, "Phone");
            var product3 = CreateProduct(3, "Keyboard");

            var cart = CreateCart(
                "user-1",

                new CartItem
                {
                    ProductId = 1,
                    Quantity = 1,
                    Product = product1
                },

                new CartItem
                {
                    ProductId = 2,
                    Quantity = 2,
                    Product = product2
                },

                new CartItem
                {
                    ProductId = 3,
                    Quantity = 3,
                    Product = product3
                });

            context.Products.AddRange(product1, product2, product3);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.GetCartItemsAsync("user-1");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal(3, result.Data.Count);

            Assert.Contains(result.Data, x =>
                x.ProductId == 1 && x.Quantity == 1);

            Assert.Contains(result.Data, x =>
                x.ProductId == 2 && x.Quantity == 2);

            Assert.Contains(result.Data, x =>
                x.ProductId == 3 && x.Quantity == 3);
        }

        // ============================================================
        // AddToCartAsync
        // ============================================================

        [Fact]
        public async Task AddToCartAsync_validItem_cartDoesNotExist_createsCartAndAddsItem()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = 2
            };

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForAddAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.AddToCartAsync(
                "user-1",
                dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var item_2 = Assert.Single(result.Data);

            Assert.Equal(1, item_2.ProductId);
            Assert.Equal(2, item_2.Quantity);

            var cart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            Assert.NotNull(cart);
            var item = Assert.Single(cart.CartItems);

            Assert.Equal(1, item.ProductId);
            Assert.Equal(2, item.Quantity);

            validator.Verify(
                v => v.ValidateForAddAsync(dto),
                Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_validItem_existingCart_addsNewItem()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var cart = CreateCart("user-1");

            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = 3
            };

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForAddAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.AddToCartAsync(
                "user-1",
                dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data);

            Assert.Equal(1, item.ProductId);
            Assert.Equal(3, item.Quantity);

            var cartInDb = await context.Carts
                .Include(c => c.CartItems)
                .FirstAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            var item_2 = Assert.Single(cartInDb.CartItems);
            Assert.Equal(3, item_2.Quantity);
        }

        [Fact]
        public async Task AddToCartAsync_existingProductInCart_increasesQuantity()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product = CreateProduct(1);

            var cart = CreateCart(
                "user-1",
                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product
                });

            context.Products.Add(product);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = 3
            };

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForAddAsync(dto))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.AddToCartAsync(
                "user-1",
                dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            var item1 = Assert.Single(result.Data);

            // Existing quantity 2 + new quantity 3
            Assert.Equal(5, item1.Quantity);

            var cartInDb = await context.Carts
                .Include(c => c.CartItems)
                .FirstAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            var item2 = Assert.Single(cartInDb.CartItems);
            Assert.Equal(5, item2.Quantity);
        }

        [Fact]
        public async Task AddToCartAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var dto = new CartItemDto
            {
                ProductId = 1,
                Quantity = 0
            };

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForAddAsync(dto))
                .ReturnsAsync(
                    ValidationResult.Invalid(
                        "Quantity must be greater than zero.",
                        ServiceErrorType.Validation));

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.AddToCartAsync(
                "user-1",
                dto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);

            Assert.Equal(
                "Quantity must be greater than zero.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.Validation,
                result.ErrorType);

            Assert.Empty(context.Carts);

            validator.Verify(
                v => v.ValidateForAddAsync(dto),
                Times.Once);
        }

        // ============================================================
        // UpdateCartItemAsync
        // ============================================================

        [Fact]
        public async Task UpdateCartItemAsync_validItem_updatesQuantity()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product = CreateProduct(1);

            var cart = CreateCart(
                "user-1",
                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product
                });

            context.Products.Add(product);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForUpdateAsync(1, 5))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.UpdateCartItemAsync(
                "user-1",
                1,
                5);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);

            Assert.Equal(1, result.Data.ProductId);
            Assert.Equal(5, result.Data.Quantity);

            var itemInDb = await context.CartItems
                .FirstOrDefaultAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);

            Assert.NotNull(itemInDb);
            Assert.Equal(5, itemInDb.Quantity);

            validator.Verify(
                v => v.ValidateForUpdateAsync(1, 5),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCartItemAsync_cartDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForUpdateAsync(1, 5))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.UpdateCartItemAsync(
                "user-1",
                1,
                5);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);

            Assert.Equal(
                "Cart not found.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        [Fact]
        public async Task UpdateCartItemAsync_itemDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var cart = CreateCart("user-1");

            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForUpdateAsync(1, 5))
                .ReturnsAsync(ValidationResult.Valid());

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.UpdateCartItemAsync(
                "user-1",
                1,
                5);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);

            Assert.Equal(
                "Item not found in cart.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        [Fact]
        public async Task UpdateCartItemAsync_validationFails_returnsValidationError()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product = CreateProduct(1);

            var cart = CreateCart(
                "user-1",
                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product
                });

            context.Products.Add(product);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();

            var validator = new Mock<ICartValidator>();

            validator
                .Setup(v => v.ValidateForUpdateAsync(1, 0))
                .ReturnsAsync(
                    ValidationResult.Invalid(
                        "Quantity must be greater than zero.",
                        ServiceErrorType.Validation));

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.UpdateCartItemAsync(
                "user-1",
                1,
                0);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);

            Assert.Equal(
                "Quantity must be greater than zero.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.Validation,
                result.ErrorType);

            var itemInDb = await context.CartItems
                .FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);

            Assert.Equal(2, itemInDb.Quantity);
        }

        // ============================================================
        // ClearCartAsync
        // ============================================================

        [Fact]
        public async Task ClearCartAsync_existingCart_removesAllItems()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product1 = CreateProduct(1);
            var product2 = CreateProduct(2, "Phone");

            var cart = CreateCart(
                "user-1",

                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product1
                },

                new CartItem
                {
                    ProductId = 2,
                    Quantity = 3,
                    Product = product2
                });

            context.Products.AddRange(product1, product2);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.ClearCartAsync("user-1");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);

            var items = await context.CartItems.ToListAsync(TestContext.Current.CancellationToken);

            Assert.Empty(items);

            var cartInDb = await context.Carts
                .Include(c => c.CartItems)
                .FirstAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            Assert.Empty(cartInDb.CartItems);
        }

        [Fact]
        public async Task ClearCartAsync_cartDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.ClearCartAsync("user-1");

            // Assert
            Assert.False(result.Success);
            Assert.False(result.Data);

            Assert.Equal(
                "Cart not found.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        // ============================================================
        // RemoveFromCartAsync
        // ============================================================

        [Fact]
        public async Task RemoveFromCartAsync_existingItem_removesItem()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var product1 = CreateProduct(1);
            var product2 = CreateProduct(2, "Phone");

            var cart = CreateCart(
                "user-1",

                new CartItem
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = product1
                },

                new CartItem
                {
                    ProductId = 2,
                    Quantity = 3,
                    Product = product2
                });

            context.Products.AddRange(product1, product2);
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.RemoveFromCartAsync(
                "user-1",
                1);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);

            var cartInDb = await context.Carts
                .Include(c => c.CartItems)
                .FirstAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);

            var item = Assert.Single(cartInDb.CartItems);

            Assert.Equal(2, item.ProductId);
        }

        [Fact]
        public async Task RemoveFromCartAsync_cartDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.RemoveFromCartAsync(
                "user-1",
                1);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.Data);

            Assert.Equal(
                "Cart not found.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }

        [Fact]
        public async Task RemoveFromCartAsync_itemDoesNotExist_returnsNotFound()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            var cart = CreateCart("user-1");

            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var mapper = CreateMapper();
            var validator = new Mock<ICartValidator>();

            var service = new CartService(
                context,
                mapper,
                validator.Object);

            // Act
            var result = await service.RemoveFromCartAsync(
                "user-1",
                999);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.Data);

            Assert.Equal(
                "Item not found in cart.",
                result.ErrorMessage);

            Assert.Equal(
                ServiceErrorType.NotFound,
                result.ErrorType);
        }
    }
}