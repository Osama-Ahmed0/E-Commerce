using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services;

public interface ICartService
{
    Task<ServiceResult<List<CartItemDto>>> GetCartItemsAsync(string userId);
    Task<ServiceResult<List<CartItemDto>>> AddToCartAsync(string userId, CartItemDto dto);
    Task<ServiceResult<CartItemDto>> UpdateCartItemAsync(string userId, int productId, int quantity);
    Task<ServiceResult<bool>> ClearCartAsync(string userId);
    Task<ServiceResult<bool>> RemoveFromCartAsync(string userId, int productId);
}
