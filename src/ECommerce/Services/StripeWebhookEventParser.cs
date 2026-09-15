using Stripe;

namespace ECommerce.Services
{
    public class StripeWebhookEventParser : IStripeWebhookEventParser
    {
        public Event ConstructEvent(string json, string signatureHeader, string webhookSecret) =>
            EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
    }
}
