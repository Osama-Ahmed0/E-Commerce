using ECommerce.Common;
using ECommerce.Dtos;
using ECommerce.Extensions;
using ECommerce.Filters;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(IOrderService service, IPaymentService paymentService, ILogger<OrderController> logger) : ControllerBase
    {
        private readonly IOrderService service = service;
        private readonly IPaymentService paymentService = paymentService;
        private readonly ILogger<OrderController> logger = logger;

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> CreateOrder(CheckoutRequestDto request)
        {
            var userId = User.GetUserId();

            if (userId == null)
                return Unauthorized();

            var result = await service.CreateOrderAsync(userId, request);
            if (!result.Success)
                return result.ToActionResult(this);
            return CreatedAtAction(nameof(GetOrder), new { id = result.Data!.Id }, result.Data);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            ServiceResult<List<OrderDto>> result;
            var userRole = User.GetRole();
            var userId = User.GetUserId();

            if (userId == null)
                return Unauthorized();

            if (userRole == "Admin")
                result = await service.GetOrdersAsync();
            else
                result = await service.GetOrdersAsync(userId);

            return result.ToActionResult(this);
        }

        [HttpGet("{id}")]
        [Authorize]
        [ServiceFilter(typeof(OrderOwnershipFilter))]
        public async Task<IActionResult> GetOrder(int id)
        {
            var result = await service.GetOrderByIdAsync(id);
            return result.ToActionResult(this);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
        {
            var result = await service.UpdateOrderStatusAsync(id, dto);
            return result.ToActionResult(this);
        }

        [HttpPut("{id}/cancel")]
        [Authorize]
        [ServiceFilter(typeof(OrderOwnershipFilter))]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var result = await service.CancelOrderAsync(id);
            return result.ToActionResult(this);
        }

        [HttpPost("{id}/payment-intent")]
        [Authorize]
        [ServiceFilter(typeof(OrderOwnershipFilter))]
        public async Task<IActionResult> CreatePaymentIntent(int id)
        {
            var result = await paymentService.CreatePaymentIntentAsync(id);
            return result.ToActionResult(this);
        }
    }
}
