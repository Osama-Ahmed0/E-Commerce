using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController(ICartService service) : ControllerBase
    {
        private readonly ICartService service = service;

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();
            var result = await service.GetCartItemsAsync(userId);
            return result.ToActionResult(this);
        }

        [Authorize]
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart(CartItemDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();
            var result = await service.AddToCartAsync(userId, dto);
            return result.ToActionResult(this);
        }

        [Authorize]
        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateCartItem(int productId, [FromQuery] int quantity)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();
            var result = await service.UpdateCartItemAsync(userId, productId, quantity);
            return result.ToActionResult(this);
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();
            var result = await service.ClearCartAsync(userId);
            return result.ToActionResult(this);
        }

        [Authorize]
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized();
            var result = await service.RemoveFromCartAsync(userId, productId);
            return result.ToActionResult(this);
        }
    }
}
