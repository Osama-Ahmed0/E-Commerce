using AutoMapper;
using ECommerce.Common;
using ECommerce.Data;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services;

public class CartService(AppDbContext context, IMapper mapper, ICartValidator validator) : ICartService
{
    private readonly AppDbContext context = context;
    private readonly IMapper mapper = mapper;
    private readonly ICartValidator validator = validator;

    public async Task<ServiceResult<List<CartItemDto>>> GetCartItemsAsync(string userId)
    {
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

        var items = mapper.Map<List<CartItemDto>>(cart.CartItems);

        return ServiceResult<List<CartItemDto>>.Ok(items);
    }

    public async Task<ServiceResult<List<CartItemDto>>> AddToCartAsync(string userId, CartItemDto dto)
    {
        var validation = await validator.ValidateForAddAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<List<CartItemDto>>.Fail(validation.ErrorMessage!, validation.ErrorType);

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
            cart.CartItems.Add(mapper.Map<CartItem>(dto));

        await context.SaveChangesAsync();

        var items = mapper.Map<List<CartItemDto>>(cart.CartItems);

        return ServiceResult<List<CartItemDto>>.Ok(items);
    }

    public async Task<ServiceResult<CartItemDto>> UpdateCartItemAsync(string userId, int productId, int quantity)
    {
        var validation = await validator.ValidateForUpdateAsync(productId, quantity);
        if (!validation.IsValid)
            return ServiceResult<CartItemDto>.Fail(validation.ErrorMessage!, validation.ErrorType);

        var cart = await context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return ServiceResult<CartItemDto>.Fail("Cart not found.", ServiceErrorType.NotFound);

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (cartItem == null)
            return ServiceResult<CartItemDto>.Fail("Item not found in cart.", ServiceErrorType.NotFound);

        cartItem.Quantity = quantity;
        await context.SaveChangesAsync();

        return ServiceResult<CartItemDto>.Ok(mapper.Map<CartItemDto>(cartItem));
    }

    public async Task<ServiceResult<bool>> ClearCartAsync(string userId)
    {
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
