using ECommerce.Common;
using ECommerce.Configuration;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Services;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stripe;

namespace ECommerce.Tests.Services
{
    public class PaymentServiceTests
    {
        private static Event BuildPaymentIntentEvent(
            string type,
            string intentId,
            string status,
            Dictionary<string, string>? metadata = null)
        {
            return new Event
            {
                Id = "evt_test_1",
                Type = type,
                Data = new Stripe.EventData
                {
                    Object = new PaymentIntent
                    {
                        Id = intentId,
                        Status = status,
                        Metadata = metadata ?? new Dictionary<string, string>()
                    }
                }
            };
        }

        private static PaymentService CreateService(
            AppDbContext context,
            Mock<IStripeWebhookEventParser>? eventParser = null,
            Mock<IOutputCacheStore>? cacheStore = null,
            StripeOptions? stripeOptions = null)
        {
            return new PaymentService(
                context,
                stripeOptions ?? new StripeOptions { SecretKey = "sk_test", WebhookSecret = "whsec_test" },
                NullLogger<PaymentService>.Instance,
                (cacheStore ?? new Mock<IOutputCacheStore>()).Object,
                (eventParser ?? new Mock<IStripeWebhookEventParser>()).Object);
        }

        // ---------- CreatePaymentIntentAsync ----------

        [Fact]
        public async Task CreatePaymentIntentAsync_orderNotFound_returnsNotFoundError()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            var service = CreateService(context);

            // Act
            var result = await service.CreatePaymentIntentAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Order not found.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        }

        [Theory]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public async Task CreatePaymentIntentAsync_orderNotPending_returnsConflictError(OrderStatus status)
        {
            // Arrange
            using var context = new InMemoryDbContext();
            context.Orders.Add(new Order
            {
                Id = 1,
                Status = status,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 100m
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = CreateService(context);

            // Act
            var result = await service.CreatePaymentIntentAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Order is not awaiting payment.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        }

        // NOTE: CreatePaymentIntentAsync's success path (and the "reuse existing intent"
        // branch) call the real Stripe PaymentIntentService directly — there's no seam
        // for that yet, so those paths aren't covered here. If you want that covered too,
        // it'd need the same kind of thin-wrapper treatment as IStripeWebhookEventParser.

        // ---------- HandleWebhookEventAsync ----------

        [Fact]
        public async Task HandleWebhookEventAsync_invalidSignature_returnsBadRequestError()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<StripeException>();

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "bad-signature");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid webhook signature.", result.ErrorMessage);
            Assert.Equal(ServiceErrorType.BadRequest, result.ErrorType);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_unhandledEventType_returnsOkWithoutSideEffects()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            var stripeEvent = new Event { Id = "evt_1", Type = "charge.refunded" };

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var cacheStore = new Mock<IOutputCacheStore>();
            var service = CreateService(context, eventParser, cacheStore);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
            cacheStore.Verify(c => c.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_succeededEventMissingOrderIdMetadata_returnsOkWithoutSideEffects()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            var stripeEvent = BuildPaymentIntentEvent(EventTypes.PaymentIntentSucceeded, "pi_1", "succeeded");

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_succeededEventOrderNotFound_returnsOkWithoutSideEffects()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentSucceeded, "pi_1", "succeeded",
                new Dictionary<string, string> { ["OrderId"] = "999" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_succeededEventPaymentIntentMismatch_doesNotUpdateOrder()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            context.Orders.Add(new Order
            {
                Id = 1,
                Status = OrderStatus.Pending,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 100m,
                StripePaymentIntentId = "pi_other"
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentSucceeded, "pi_1", "succeeded",
                new Dictionary<string, string> { ["OrderId"] = "1" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            var order = await context.Orders.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Pending, order!.Status);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_succeededEventPendingOrder_marksOrderAsPaid()
        {
            // Arrange
            using var context = new InMemoryDbContext();
            context.Orders.Add(new Order
            {
                Id = 1,
                Status = OrderStatus.Pending,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 100m,
                StripePaymentIntentId = "pi_1"
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentSucceeded, "pi_1", "succeeded",
                new Dictionary<string, string> { ["OrderId"] = "1" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            var order = await context.Orders.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Paid, order!.Status);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_succeededEventAlreadyPaid_isIdempotent()
        {
            // Arrange — simulates Stripe redelivering the same succeeded event.
            using var context = new InMemoryDbContext();
            context.Orders.Add(new Order
            {
                Id = 1,
                Status = OrderStatus.Paid,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 100m,
                StripePaymentIntentId = "pi_1"
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentSucceeded, "pi_1", "succeeded",
                new Dictionary<string, string> { ["OrderId"] = "1" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var service = CreateService(context, eventParser);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);
            var order = await context.Orders.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Paid, order!.Status);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_paymentFailedPendingOrder_restoresStockAndCartAndCancelsOrder()
        {
            // Arrange
            using var context = new InMemoryDbContext();

            context.Products.Add(new Data.Models.Product { Id = 1, Name = "Widget", Stock = 5, Price = 10m });

            context.Orders.Add(new Order
            {
                Id = 1,
                Status = OrderStatus.Pending,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 30m,
                StripePaymentIntentId = "pi_1",
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = 1, Quantity = 3, UnitPrice = 10m }
                }
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentPaymentFailed, "pi_1", "requires_payment_method",
                new Dictionary<string, string> { ["OrderId"] = "1" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var cacheStore = new Mock<IOutputCacheStore>();
            var service = CreateService(context, eventParser, cacheStore);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);

            var order = await context.Orders.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Cancelled, order!.Status);

            var product = await context.Products.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(8, product!.Stock); // 5 + 3 restored

            var cart = await context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == "user-1", TestContext.Current.CancellationToken);
            Assert.NotNull(cart);
            var cartItem = Assert.Single(cart!.CartItems);
            Assert.Equal(1, cartItem.ProductId);
            Assert.Equal(3, cartItem.Quantity);

            cacheStore.Verify(c => c.EvictByTagAsync("products", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleWebhookEventAsync_paymentFailedNonPendingOrder_doesNotRestoreOrEvictCache()
        {
            // Arrange — order already moved past Pending (e.g. previously cancelled);
            // a late/duplicate payment_intent.payment_failed should be a no-op.
            using var context = new InMemoryDbContext();

            context.Products.Add(new Data.Models.Product { Id = 1, Name = "Widget", Stock = 5, Price = 10m });

            context.Orders.Add(new Order
            {
                Id = 1,
                Status = OrderStatus.Cancelled,
                UserId = "user-1",
                ShippingAddress = "123 Main Street",
                TotalAmount = 30m,
                StripePaymentIntentId = "pi_1",
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = 1, Quantity = 3, UnitPrice = 10m }
                }
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var stripeEvent = BuildPaymentIntentEvent(
                EventTypes.PaymentIntentPaymentFailed, "pi_1", "requires_payment_method",
                new Dictionary<string, string> { ["OrderId"] = "1" });

            var eventParser = new Mock<IStripeWebhookEventParser>();
            eventParser
                .Setup(p => p.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(stripeEvent);

            var cacheStore = new Mock<IOutputCacheStore>();
            var service = CreateService(context, eventParser, cacheStore);

            // Act
            var result = await service.HandleWebhookEventAsync("{}", "sig");

            // Assert
            Assert.True(result.Success);

            var order = await context.Orders.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(OrderStatus.Cancelled, order!.Status);

            var product = await context.Products.FindAsync([1, TestContext.Current.CancellationToken], TestContext.Current.CancellationToken);
            Assert.Equal(5, product!.Stock);

            cacheStore.Verify(c => c.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}