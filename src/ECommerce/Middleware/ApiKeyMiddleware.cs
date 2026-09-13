namespace ECommerce.Middleware
{
    public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        private readonly RequestDelegate next = next;
        private readonly IConfiguration configuration = configuration;
        private const string ApiKeyHeaderName = "X-API-Key";

        private static readonly string[] ExcludedPathPrefixes = ["/scalar", "/openapi", "/health", "/api/payments/webhook"];

        public async Task InvokeAsync(HttpContext context)
        {
            if (ExcludedPathPrefixes.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }

            var apiKey = configuration["ApiKey"];

            if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await next(context);
        }
    }
}
