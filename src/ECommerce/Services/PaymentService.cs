using ECommerce.Common;
using ECommerce.Configuration;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ECommerce.Services
{
    public class PaymentService(
        AppDbContext context,
        StripeOptions stripeOptions,
        ILogger<PaymentService> logger,
        IOutputCacheStore cacheStore,
        IStripeWebhookEventParser eventParser) : IPaymentService
    {
        private readonly AppDbContext context = context;
        private readonly StripeOptions stripeOptions = stripeOptions;
        private readonly ILogger<PaymentService> logger = logger;
        private readonly IOutputCacheStore cacheStore = cacheStore;
        private readonly IStripeWebhookEventParser eventParser = eventParser;

        public async Task<ServiceResult<PaymentIntentResponseDto>> CreatePaymentIntentAsync(int orderId)
        {
            var order = await context.Orders.FindAsync(orderId);
            if (order == null)
                return ServiceResult<PaymentIntentResponseDto>.Fail("Order not found.", ServiceErrorType.NotFound);

            if (order.Status != OrderStatus.Pending)
                return ServiceResult<PaymentIntentResponseDto>.Fail("Order is not awaiting payment.", ServiceErrorType.Conflict);

            var piService = new PaymentIntentService();

            try
            {
                // Already has an intent (e.g. page refresh) — reuse it instead of creating a duplicate charge target.
                if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
                {
                    var existing = await piService.GetAsync(order.StripePaymentIntentId);
                    return ServiceResult<PaymentIntentResponseDto>.Ok(new PaymentIntentResponseDto
                    {
                        ClientSecret = existing.ClientSecret
                    });
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(order.TotalAmount * 100), // Stripe wants the smallest currency unit (cents)
                    Currency = "usd",
                    Metadata = new Dictionary<string, string> { ["OrderId"] = order.Id.ToString() },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },

                };

                // Idempotency key: a network retry of this exact call won't create a second PaymentIntent.
                var requestOptions = new RequestOptions { IdempotencyKey = $"order-{order.Id}-create-intent" };

                var intent = await piService.CreateAsync(options, requestOptions);

                order.StripePaymentIntentId = intent.Id;
                await context.SaveChangesAsync();

                return ServiceResult<PaymentIntentResponseDto>.Ok(new PaymentIntentResponseDto { ClientSecret = intent.ClientSecret });
            }
            catch (StripeException ex)
            {
                return ServiceResult<PaymentIntentResponseDto>.Fail(ex.Message, ServiceErrorType.BadRequest);
            }
        }
        public async Task<ServiceResult<bool>> HandleWebhookEventAsync(string json, string signatureHeader)
        {
            Event stripeEvent;

            try
            {
                stripeEvent = eventParser.ConstructEvent(json, signatureHeader, stripeOptions.WebhookSecret!);
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "Invalid Stripe webhook signature.");

                return ServiceResult<bool>.Fail("Invalid webhook signature.", ServiceErrorType.BadRequest);
            }

            logger.LogInformation("Received Stripe event: {EventType}, EventId: {EventId}", stripeEvent.Type, stripeEvent.Id);

            if (stripeEvent.Type != EventTypes.PaymentIntentSucceeded && stripeEvent.Type != EventTypes.PaymentIntentPaymentFailed)
                return ServiceResult<bool>.Ok(true);

            if (stripeEvent.Data.Object is not PaymentIntent intent)
            {
                logger.LogInformation("Ignoring Stripe event {EventType} because it is not a PaymentIntent.", stripeEvent.Type);

                return ServiceResult<bool>.Ok(true);
            }

            logger.LogInformation("PaymentIntent: {PaymentIntentId}, Status: {Status}", intent.Id, intent.Status);

            if (!intent.Metadata.TryGetValue("OrderId", out var orderIdString) || !int.TryParse(orderIdString, out var orderId))
            {
                logger.LogError("PaymentIntent {PaymentIntentId} has no valid OrderId metadata.", intent.Id);

                return ServiceResult<bool>.Ok(true);
            }

            logger.LogInformation("Stripe PaymentIntent {PaymentIntentId} belongs to Order {OrderId}", intent.Id, orderId);

            var order = await context.Orders.FindAsync(orderId);

            if (order == null)
            {
                logger.LogError("Order {OrderId} not found for PaymentIntent {PaymentIntentId}.", orderId, intent.Id);

                return ServiceResult<bool>.Ok(true);
            }

            if (order.StripePaymentIntentId != intent.Id)
            {
                logger.LogError("PaymentIntent mismatch for Order {OrderId}. Database: {DatabasePaymentIntentId}, Stripe: {StripePaymentIntentId}",
                    order.Id, order.StripePaymentIntentId, intent.Id);

                return ServiceResult<bool>.Ok(true);
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Paid;

                    await context.SaveChangesAsync();

                    logger.LogInformation("Order {OrderId} marked as Paid.", order.Id);
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                logger.LogWarning("Payment failed for Order {OrderId}, PaymentIntent {PaymentIntentId}", order.Id, intent.Id);

                if (order.Status == OrderStatus.Pending)
                    await RestoreStockAndCartAsync(order);
            }

            return ServiceResult<bool>.Ok(true);
        }

        // Undoes what CreateOrderAsync reserved: gives the stock back to each product
        // and re-adds the items to the user's cart, then cancels the order.
        private async Task RestoreStockAndCartAsync(Order order)
        {
            await context.Entry(order)
                .Collection(o => o.OrderItems)
                .Query()
                .Include(oi => oi.Product)
                .LoadAsync();

            var cart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == order.UserId);

            if (cart == null)
            {
                cart = new Cart { UserId = order.UserId };
                context.Carts.Add(cart);
            }

            foreach (var item in order.OrderItems)
            {
                item.Product.Stock += item.Quantity;

                var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == item.ProductId);
                if (existingCartItem != null)
                    existingCartItem.Quantity += item.Quantity;
                else
                    cart.CartItems.Add(new CartItem { ProductId = item.ProductId, Quantity = item.Quantity });
            }

            order.Status = OrderStatus.Cancelled;

            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                await context.SaveChangesAsync();
                await tx.CommitAsync();

                await cacheStore.EvictByTagAsync("products", default);

                logger.LogInformation("Order {OrderId} cancelled after payment failure; stock restored and cart re-populated for user {UserId}.", order.Id, order.UserId);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Failed to restore stock/cart for Order {OrderId} after payment failure.", order.Id);
            }
        }
    }
}
