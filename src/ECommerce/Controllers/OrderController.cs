using ECommerce.Common;
using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(IOrderService service) : ControllerBase
    {
        private readonly IOrderService service = service;

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> CreateOrder(CheckoutRequestDto request)
        {
            var userId = User.GetUserId();

            if (userId == null)
                return Unauthorized();

            var result = await service.CreateOrder(userId, request.ShippingAddress);
            if (!result.Success)
                return result.ToActionResult(this);
            return CreatedAtAction(nameof(GetOrder), new { id = result.Data!.Id }, result.Data);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            ServiceResult<List<OrderDto>> result;
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = User.GetUserId();

            if (userId == null)
                return Unauthorized();

            if (userRole == "Customer")
                result = await service.GetOrders(userId);
            else if (userRole == "Admin")
                result = await service.GetOrders();
            else
                return Forbid();

            return result.ToActionResult(this);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            var result = await service.GetOrderById(id);
            return result.ToActionResult(this);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var result = await service.UpdateOrderStatus(id, status);
            return result.ToActionResult(this);
        }

        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var result = await service.CancelOrder(id);
            return result.ToActionResult(this);
        }
    }
}
