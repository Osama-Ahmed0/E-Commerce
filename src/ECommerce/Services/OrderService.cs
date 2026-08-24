using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Dtos;
using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ECommerce.Services.Validation;

namespace ECommerce.Services
{
    public class OrderService(AppDbContext context, IMapper mapper, IOrderValidator validator) : IOrderService
    {
        private readonly AppDbContext context = context;
        private readonly IMapper mapper = mapper;
        private readonly IOrderValidator validator = validator;

        public async Task<ServiceResult<OrderDto>> CreateOrderAsync(string userId, CheckoutRequestDto dto)
        {
            var validationResult = await validator.ValidateForCreateAsync(dto);
            if (!validationResult.IsValid)
                return ServiceResult<OrderDto>.Fail(validationResult.ErrorMessage!, validationResult.ErrorType);

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
                ShippingAddress = dto.ShippingAddress,
                OrderDate = DateTime.UtcNow,
                UserId = userId,
                TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price),
                OrderItems = cart.CartItems.Select(mapper.Map<OrderItem>).ToList()
            };

            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                context.Orders.Add(order);

                context.CartItems.RemoveRange(cart.CartItems);

                await context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                return ServiceResult<OrderDto>.Fail("The cart or product stock changed while placing your order. Please try again.", ServiceErrorType.Conflict);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                return ServiceResult<OrderDto>.Fail("Could not create order.", ServiceErrorType.BadRequest);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            var orderDto = mapper.Map<OrderDto>(order);
            return ServiceResult<OrderDto>.Ok(orderDto);
        }

        public async Task<ServiceResult<List<OrderDto>>> GetOrdersAsync(string userId)
        {
            var orders = await OrdersWithItems(userId).ToListAsync();

            var dtos = orders.Select(mapper.Map<OrderDto>).ToList();
            return ServiceResult<List<OrderDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<List<OrderDto>>> GetOrdersAsync()
        {
            var orders = await OrdersWithItems().ToListAsync();

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

        public async Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
        {
            var validationResult = await validator.ValidateForUpdateStatusAsync(dto);
            if (!validationResult.IsValid)
                return ServiceResult<OrderDto>.Fail(validationResult.ErrorMessage!, validationResult.ErrorType);

            var requested = Enum.Parse<OrderStatus>(dto.OrderStatus, true);

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

        private IQueryable<Order> OrdersWithItems(string? userId = null)
        {
            var query = context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (userId != null)
                query = query.Where(o => o.UserId == userId);

            query = query.OrderByDescending(o => o.OrderDate);
            return query;
        }
    }
}