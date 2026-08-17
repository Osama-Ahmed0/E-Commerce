using ECommerce.Common;
using ECommerce.Dtos;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var result = await service.GetCartItemsAsync(userId);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart(CartItemDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            var result = await service.AddToCartAsync(userId, dto);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateCartItem(int productId, [FromQuery] int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            var result = await service.UpdateCartItemAsync(userId, productId, quantity);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            var result = await service.ClearCartAsync(userId);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            return Ok();
        }

        [Authorize]
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            var result = await service.RemoveFromCartAsync(userId, productId);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            return Ok();
        }
    }
}
