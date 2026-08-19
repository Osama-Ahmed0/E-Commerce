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
    public class OrderController(IOrderService service) : ControllerBase
    {
        private readonly IOrderService service = service;

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] string shippingAddress)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await service.CreateOrder(userId, shippingAddress);
            if (!result.Success)
            {
                // map error types to appropriate responses
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage)
                    : BadRequest(result.ErrorMessage);
            }

            return CreatedAtAction(nameof(GetOrder), new { id = result.Data!.Id }, result.Data);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            ServiceResult<List<OrderDto>> result;
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userRole == "Customer")
                result = await service.GetOrders(userId);
            else if (userRole == "Admin")
                result = await service.GetOrders();
            else
                return Forbid();

            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            var result = await service.GetOrderById(id);
            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var result = await service.UpdateOrderStatus(id, status);
            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }

        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var result = await service.CancelOrder(id);
            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return Ok(result.Data);
        }
    }
}
