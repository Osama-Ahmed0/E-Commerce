using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services;

public class CartService(AppDbContext context) : ICartService
{
    private readonly AppDbContext context = context;

    public async Task<ServiceResult<List<CartItemDto>>> GetCartItemsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<List<CartItemDto>>.Fail("Invalid user", ServiceErrorType.BadRequest);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        var items = cart.CartItems.Select(ci => new CartItemDto
        {
            ProductId = ci.ProductId,
            Quantity = ci.Quantity
        }).ToList();

        return ServiceResult<List<CartItemDto>>.Ok(items);
    }

    public async Task<ServiceResult<List<CartItemDto>>> AddToCartAsync(string userId, CartItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(userId) || dto == null)
            return ServiceResult<List<CartItemDto>>.Fail("Invalid data", ServiceErrorType.BadRequest);

        if (dto.Quantity <= 0)
            return ServiceResult<List<CartItemDto>>.Fail("Quantity must be greater than zero", ServiceErrorType.Validation);

        var productExists = await context.Products.FindAsync(dto.ProductId);
        if (productExists == null)
            return ServiceResult<List<CartItemDto>>.Fail($"Product {dto.ProductId} not found.", ServiceErrorType.NotFound);

        if (dto.Quantity > productExists.Stock)
            return ServiceResult<List<CartItemDto>>.Fail($"Quantity exceeds available stock for product {dto.ProductId}.", ServiceErrorType.Validation);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            context.Carts.Add(cart);
        }

        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

        if (existing != null)
            existing.Quantity += dto.Quantity;

        else
            cart.CartItems.Add(new CartItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            });

        await context.SaveChangesAsync();

        var items = cart.CartItems.Select(ci => new CartItemDto
        {
            ProductId = ci.ProductId,
            Quantity = ci.Quantity
        }).ToList();

        return ServiceResult<List<CartItemDto>>.Ok(items);
    }

    public async Task<ServiceResult<CartItemDto>> UpdateCartItemAsync(string userId, int productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<CartItemDto>.Fail("Invalid user", ServiceErrorType.BadRequest);

        if (quantity <= 0)
            return ServiceResult<CartItemDto>.Fail("Quantity must be greater than zero", ServiceErrorType.Validation);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return ServiceResult<CartItemDto>.Fail("Cart not found.", ServiceErrorType.NotFound);

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (cartItem == null)
            return ServiceResult<CartItemDto>.Fail("Item not found in cart.", ServiceErrorType.NotFound);

        var product = await context.Products.FindAsync(productId);

        if (product?.Stock < quantity)
            return ServiceResult<CartItemDto>.Fail($"Quantity exceeds available stock for product {productId}.", ServiceErrorType.Validation);

        cartItem.Quantity = quantity;
        await context.SaveChangesAsync();

        return ServiceResult<CartItemDto>.Ok(new CartItemDto
        {
            ProductId = cartItem.ProductId,
            Quantity = cartItem.Quantity
        });
    }

    public async Task<ServiceResult<bool>> ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<bool>.Fail("Invalid user", ServiceErrorType.BadRequest);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return ServiceResult<bool>.Fail("Cart not found.", ServiceErrorType.NotFound);

        context.CartItems.RemoveRange(cart.CartItems);
        await context.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> RemoveFromCartAsync(string userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<bool>.Fail("Invalid user", ServiceErrorType.BadRequest);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return ServiceResult<bool>.Fail("Cart not found.", ServiceErrorType.NotFound);

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (cartItem == null)
            return ServiceResult<bool>.Fail("Item not found in cart.", ServiceErrorType.NotFound);

        cart.CartItems.Remove(cartItem);
        await context.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }
}
