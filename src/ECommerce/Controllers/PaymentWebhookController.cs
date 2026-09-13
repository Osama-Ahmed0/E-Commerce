using ECommerce.Extensions;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentWebhookController(IPaymentService service) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var result = await service.HandleWebhookEventAsync(json, signature);
            return result.ToActionResult(this);
        }
    }
}
