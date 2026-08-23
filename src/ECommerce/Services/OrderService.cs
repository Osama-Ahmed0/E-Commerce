using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Dtos;
using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace ECommerce.Services
{
    public class OrderService(AppDbContext context, IMapper mapper) : IOrderService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;

        public async Task<ServiceResult<OrderDto>> CreateOrderAsync(string userId, string shippingAddress)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult<OrderDto>.Fail("User id is required.", ServiceErrorType.Validation);
            if (string.IsNullOrWhiteSpace(shippingAddress))
                return ServiceResult<OrderDto>.Fail("Shipping address is required.", ServiceErrorType.Validation);

            var cart = await context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems.Count == 0)
                return ServiceResult<OrderDto>.Fail("Cart is empty.", ServiceErrorType.Validation);

            foreach (var ci in cart.CartItems)
            {
                var product = ci.Product;
                if (product == null)
                    return ServiceResult<OrderDto>.Fail($"Product {ci.ProductId} not found.", ServiceErrorType.NotFound);

                if (product.Stock < ci.Quantity)
                    return ServiceResult<OrderDto>.Fail($"Insufficient stock for product '{product.Name}' (id {product.Id}).", ServiceErrorType.Validation);

                product.Stock -= ci.Quantity;

                context.Products.Update(product);
            }

            var order = new Order
            {
                Status = OrderStatus.Pending,
                ShippingAddress = shippingAddress,
                OrderDate = DateTime.UtcNow,
                UserId = userId,
                TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price),
                OrderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Product = ci.Product,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price
                }).ToList()
            };

            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                context.Orders.Add(order);

                context.CartItems.RemoveRange(cart.CartItems);

                await context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                return ServiceResult<OrderDto>.Fail("Could not create order.", ServiceErrorType.BadRequest);
            }

            var dto = mapper.Map<OrderDto>(order);
            return ServiceResult<OrderDto>.Ok(dto);
        }

        public async Task<ServiceResult<List<OrderDto>>> GetOrdersAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult<List<OrderDto>>.Fail("User id is required.", ServiceErrorType.Validation);

            var orders = await context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var dtos = orders.Select(mapper.Map<OrderDto>).ToList();
            return ServiceResult<List<OrderDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<List<OrderDto>>> GetOrdersAsync()
        {
            var orders = await context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var dtos = orders.Select(mapper.Map<OrderDto>).ToList();
            return ServiceResult<List<OrderDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<OrderDto>> GetOrderByIdAsync(int orderId)
        {
            var order = await context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return ServiceResult<OrderDto>.Fail("Order not found.", ServiceErrorType.NotFound);

            return ServiceResult<OrderDto>.Ok(mapper.Map<OrderDto>(order));
        }

        public async Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return ServiceResult<OrderDto>.Fail("Status is required.", ServiceErrorType.Validation);

            if (!Enum.TryParse<OrderStatus>(status, true, out var requested))
                return ServiceResult<OrderDto>.Fail("Invalid order status.", ServiceErrorType.Validation);

            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return ServiceResult<OrderDto>.Fail("Order not found.", ServiceErrorType.NotFound);

            var current = order.Status;

            if (current == requested)
            {
                var already = await context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
                return ServiceResult<OrderDto>.Ok(mapper.Map<OrderDto>(already!));
            }

            bool allowed = current switch
            {
                OrderStatus.Pending => requested == OrderStatus.Paid || requested == OrderStatus.Cancelled,
                OrderStatus.Paid => requested == OrderStatus.Shipped || requested == OrderStatus.Cancelled,
                OrderStatus.Shipped => requested == OrderStatus.Delivered,
                OrderStatus.Delivered => false,
                OrderStatus.Cancelled => false,
                _ => false
            };

            if (!allowed)
                return ServiceResult<OrderDto>.Fail($"Invalid status transition from {current} to {requested}.", ServiceErrorType.Conflict);

            order.Status = requested;
            await context.SaveChangesAsync();

            var reloaded = await context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return ServiceResult<OrderDto>.Ok(mapper.Map<OrderDto>(reloaded!));
        }

        public async Task<ServiceResult<bool>> CancelOrderAsync(int orderId)
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return ServiceResult<bool>.Fail("Order not found.", ServiceErrorType.NotFound);

            if (order.Status != OrderStatus.Pending)
                return ServiceResult<bool>.Fail("order has already progressed past Pending and can no longer be self-cancelled.", ServiceErrorType.Conflict);

            order.Status = OrderStatus.Cancelled;
            await context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}