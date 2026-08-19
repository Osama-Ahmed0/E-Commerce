using ECommerce.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Common;
using ECommerce.Services;
using System.Security.Claims;

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
            // provide sensible defaults for paging
            var pn = pageNumber.GetValueOrDefault(1);
            var ps = pageSize.GetValueOrDefault(10);
            if (pn <= 0) pn = 1;
            if (ps <= 0 || ps > 100) ps = 10;

            var result = await service.GetReviewsAsync(productId, pn, ps);
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromRoute] int productId, [FromBody] WriteReviewDto reviewDto)
        {
            if (reviewDto is null)
                return BadRequest("Review data is required.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var result = await service.CreateReviewAsync(productId, userId, reviewDto);
            if (!result.Success)
                return result.ErrorType == ServiceErrorType.NotFound ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);

            // return created resource location - point to the reviews list for the product
            return CreatedAtAction(nameof(GetReviews), new { productId, pageNumber = 1, pageSize = 10 }, result.Data);
        }
    }
}
