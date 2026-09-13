namespace ECommerce.Configuration;

public sealed class StripeOptions
{
    public string? SecretKey { get; init; }
    public string? WebhookSecret { get; init; }
}
