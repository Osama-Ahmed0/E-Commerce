using ECommerce.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Services;
using ECommerce.Extensions;

namespace ECommerce.Controllers
{
    [Route("api/products/{productId}/reviews")]
    [ApiController]
    public class ProductReviewController(IReviewService service) : ControllerBase
    {
        private readonly IReviewService service = service;

        [HttpGet]
        public async Task<IActionResult> GetReviews([FromRoute] int productId, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var result = await service.GetReviewsAsync(productId, pageNumber, pageSize);
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromRoute] int productId, [FromBody] WriteReviewDto reviewDto)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await service.CreateReviewAsync(productId, userId, reviewDto);
            if (!result.Success)
                return result.ToActionResult(this);

            return CreatedAtAction(nameof(GetReviews), new { productId, pageNumber = 1, pageSize = 10 }, result.Data);
        }
    }
}
