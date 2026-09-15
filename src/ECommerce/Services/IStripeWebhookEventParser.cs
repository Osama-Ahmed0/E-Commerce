using Stripe;

namespace ECommerce.Services
{
    public interface IStripeWebhookEventParser
    {
        Event ConstructEvent(string json, string signatureHeader, string webhookSecret);
    }
}
