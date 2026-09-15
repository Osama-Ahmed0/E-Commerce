using AutoMapper;
using ECommerce.Common;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Mapping;
using ECommerce.Services;
using ECommerce.Services.Validation;
using ECommerce.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Services
{
    public class OrderServiceTests
    {
        private static readonly IMapper Mapper =
            new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), new LoggerFactory()).CreateMapper();

        private static Mock<IOrderValidator> ValidValidator()
        {
            var validator = new Mock<IOrderValidator>();
            validator.Setup(v => v.ValidateForCreateAsync(It.IsAny<CheckoutRequestDto>()))
                .ReturnsAsync(ValidationResult.Valid());
            validator.Setup(v => v.ValidateForUpdateStatusAsync(It.IsAny<UpdateOrderStatusDto>()))
                .ReturnsAsync(ValidationResult.Valid());
            return validator;
        }

        private static async Task<Order> SeedOrderAsync(InMemoryDbContext context, string userId, OrderStatus status)
        {
            var category = new Category { Name = $"Category-{Guid.NewGuid()}" };
            var product = new Product { Name = "Test Product", Price = 10m, Stock = 10, Category = category };

            var order = new Order
            {
                Status = status,
                ShippingAddress = "123 Main St",
                OrderDate = DateTime.UtcNow,
                UserId = userId,
                TotalAmount = 10m
            };
            order.OrderItems.Add(new OrderItem { Product = product, Quantity = 1, UnitPrice = 10m });

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            return order;
        }

        // ---------- CreateOrderAsync ----------

        [Fact]
        public async Task CreateOrderAsync_validationFails_returnsValidatorError()
        {
            using var context = new InMemoryDbContext();
            var validator = new Mock<IOrderValidator>();
            validator.Setup(v => v.ValidateForCreateAsync(It.IsAny<CheckoutRequestDto>()))
                .ReturnsAsync(ValidationResult.Invalid("Shipping address is required.", ServiceErrorType.BadRequest));

            var service = new OrderService(context, Mapper, validator.Object);

            var result = await service.CreateOrderAsync("user-1", new CheckoutRequestDto { ShippingAddress = "" });

            Assert.False(result.Success);
            Assert.Equal("Shipping address is required.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.BadRequest, result.ErrorType);
        }

        [Fact]
        public async Task CreateOrderAsync_cartDoesNotExist_returnsValidationError()
        {
            using var context = new InMemoryDbContext();
            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CreateOrderAsync("user-1", new CheckoutRequestDto { ShippingAddress = "123 Main St" });

            Assert.False(result.Success);
            Assert.Equal("Cart is empty.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Fact]
        public async Task CreateOrderAsync_cartIsEmpty_returnsValidationError()
        {
            using var context = new InMemoryDbContext();
            context.Carts.Add(new Cart { UserId = "user-1" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CreateOrderAsync("user-1", new CheckoutRequestDto { ShippingAddress = "123 Main St" });

            Assert.False(result.Success);
            Assert.Equal("Cart is empty.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Fact]
        public async Task CreateOrderAsync_insufficientStock_returnsValidationError()
        {
            using var context = new InMemoryDbContext();

            var category = new Category { Id = 1, Name = "Cat" };
            var product = new Product { Id = 1, Name = "Widget", Price = 10m, Stock = 2, Category = category };
            context.Categories.Add(category);
            context.Products.Add(product);

            var cart = new Cart { UserId = "user-1" };
            cart.CartItems.Add(new CartItem { ProductId = product.Id, Product = product, Quantity = 5 });
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CreateOrderAsync("user-1", new CheckoutRequestDto { ShippingAddress = "123 Main St" });

            Assert.False(result.Success);
            Assert.Equal("Insufficient stock for product 'Widget' (id 1).", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        }

        [Fact]
        public async Task CreateOrderAsync_validCart_createsOrderReducesStockAndClearsCart()
        {
            using var context = new InMemoryDbContext();

            var category = new Category { Id = 1, Name = "Cat" };
            var product1 = new Product { Id = 1, Name = "Widget", Price = 10m, Stock = 5, Category = category };
            var product2 = new Product { Id = 2, Name = "Gadget", Price = 20m, Stock = 3, Category = category };
            context.Categories.Add(category);
            context.Products.AddRange(product1, product2);

            var cart = new Cart { UserId = "user-1" };
            cart.CartItems.Add(new CartItem { ProductId = product1.Id, Product = product1, Quantity = 2 });
            cart.CartItems.Add(new CartItem { ProductId = product2.Id, Product = product2, Quantity = 1 });
            context.Carts.Add(cart);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CreateOrderAsync("user-1", new CheckoutRequestDto { ShippingAddress = "123 Main St" });

            Assert.True(result.Success);
            Assert.Equal(40m, result.Data!.TotalAmount); // 2*10 + 1*20
            Assert.Equal("123 Main St", result.Data.ShippingAddress);
            Assert.Equal(OrderStatus.Pending, result.Data.Status);
            Assert.Equal(2, result.Data.Items.Count());

            var updatedProduct1 = await context.Products.FindAsync([product1.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            var updatedProduct2 = await context.Products.FindAsync([product2.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(3, updatedProduct1!.Stock);
            Assert.Equal(2, updatedProduct2!.Stock);

            var remainingCartItems = await context.CartItems.Where(ci => ci.CartId == "user-1").ToListAsync(TestContext.Current.CancellationToken);
            Assert.Empty(remainingCartItems);

            var savedOrder = await context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.UserId == "user-1", TestContext.Current.CancellationToken);
            Assert.NotNull(savedOrder);
            Assert.Equal(2, savedOrder!.OrderItems.Count);
        }

        // ---------- GetOrdersAsync ----------

        [Fact]
        public async Task GetOrdersAsync_withUserId_returnsOnlyThatUsersOrders()
        {
            using var context = new InMemoryDbContext();
            await SeedOrderAsync(context, "user-1", OrderStatus.Pending);
            await SeedOrderAsync(context, "user-2", OrderStatus.Pending);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.GetOrdersAsync("user-1");

            Assert.True(result.Success);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task GetOrdersAsync_withoutUserId_returnsAllOrders()
        {
            using var context = new InMemoryDbContext();
            await SeedOrderAsync(context, "user-1", OrderStatus.Pending);
            await SeedOrderAsync(context, "user-2", OrderStatus.Pending);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.GetOrdersAsync();

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count);
        }

        // ---------- GetOrderByIdAsync ----------

        [Fact]
        public async Task GetOrderByIdAsync_existingOrder_returnsOrder()
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", OrderStatus.Pending);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.GetOrderByIdAsync(order.Id);

            Assert.True(result.Success);
            Assert.Equal(order.Id, result.Data!.Id);
        }

        [Fact]
        public async Task GetOrderByIdAsync_nonExistingOrder_returnsNotFound()
        {
            using var context = new InMemoryDbContext();
            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.GetOrderByIdAsync(999);

            Assert.False(result.Success);
            Assert.Equal("Order not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        // ---------- UpdateOrderStatusAsync ----------

        [Fact]
        public async Task UpdateOrderStatusAsync_validationFails_returnsValidatorError()
        {
            using var context = new InMemoryDbContext();
            var validator = new Mock<IOrderValidator>();
            validator.Setup(v => v.ValidateForUpdateStatusAsync(It.IsAny<UpdateOrderStatusDto>()))
                .ReturnsAsync(ValidationResult.Invalid("Invalid order status.", ServiceErrorType.Validation));

            var service = new OrderService(context, Mapper, validator.Object);

            var result = await service.UpdateOrderStatusAsync(1, new UpdateOrderStatusDto { OrderStatus = "Bogus" });

            Assert.False(result.Success);
            Assert.Equal("Invalid order status.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_orderNotFound_returnsNotFound()
        {
            using var context = new InMemoryDbContext();
            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.UpdateOrderStatusAsync(999, new UpdateOrderStatusDto { OrderStatus = "Paid" });

            Assert.False(result.Success);
            Assert.Equal("Order not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_sameStatus_returnsOkUnchanged()
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", OrderStatus.Pending);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.UpdateOrderStatusAsync(order.Id, new UpdateOrderStatusDto { OrderStatus = "Pending" });

            Assert.True(result.Success);
            Assert.Equal(OrderStatus.Pending, result.Data!.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Pending, "Paid")]
        [InlineData(OrderStatus.Pending, "Cancelled")]
        [InlineData(OrderStatus.Paid, "Shipped")]
        [InlineData(OrderStatus.Paid, "Cancelled")]
        [InlineData(OrderStatus.Shipped, "Delivered")]
        public async Task UpdateOrderStatusAsync_allowedTransition_updatesStatus(OrderStatus current, string target)
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", current);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.UpdateOrderStatusAsync(order.Id, new UpdateOrderStatusDto { OrderStatus = target });

            Assert.True(result.Success);
            Assert.Equal(Enum.Parse<OrderStatus>(target), result.Data!.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Pending, "Shipped")]
        [InlineData(OrderStatus.Pending, "Delivered")]
        [InlineData(OrderStatus.Paid, "Delivered")]
        [InlineData(OrderStatus.Shipped, "Cancelled")]
        [InlineData(OrderStatus.Delivered, "Paid")]
        [InlineData(OrderStatus.Cancelled, "Paid")]
        public async Task UpdateOrderStatusAsync_disallowedTransition_returnsConflict(OrderStatus current, string target)
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", current);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.UpdateOrderStatusAsync(order.Id, new UpdateOrderStatusDto { OrderStatus = target });

            Assert.False(result.Success);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }

        // ---------- CancelOrderAsync ----------

        [Fact]
        public async Task CancelOrderAsync_orderNotFound_returnsNotFound()
        {
            using var context = new InMemoryDbContext();
            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CancelOrderAsync(999);

            Assert.False(result.Success);
            Assert.Equal("Order not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        [Fact]
        public async Task CancelOrderAsync_orderIsPending_cancelsOrder()
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", OrderStatus.Pending);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CancelOrderAsync(order.Id);

            Assert.True(result.Success);
            Assert.True(result.Data);

            var updated = await context.Orders.FindAsync([order.Id, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Cancelled, updated!.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task CancelOrderAsync_orderPastPending_returnsConflict(OrderStatus current)
        {
            using var context = new InMemoryDbContext();
            var order = await SeedOrderAsync(context, "user-1", current);

            var service = new OrderService(context, Mapper, ValidValidator().Object);

            var result = await service.CancelOrderAsync(order.Id);

            Assert.False(result.Success);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }
    }
}