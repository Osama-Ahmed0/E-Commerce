using ECommerce.Middleware;
using System.Threading.RateLimiting;

namespace ECommerce.Extensions
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
            services.AddRateLimiting();
            services.AddApiOutputCaching();
            services.AddApiCors();
            services.AddOpenApiExtension();

            return services;
        }

        private static void AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }

        private static void AddApiOutputCaching(this IServiceCollection services)
        {
            services.AddOutputCache(options =>
            {
                options.AddPolicy("Products", policy => policy
                    .Expire(TimeSpan.FromMinutes(2))
                    .SetVaryByQuery("categoryId", "minPrice", "maxPrice", "sort", "pageNumber", "pageSize")
                    .Tag("products"));

                options.AddPolicy("Categories", policy => policy
                    .Expire(TimeSpan.FromMinutes(2))
                    .SetVaryByQuery("pageNumber", "pageSize")
                    .Tag("categories"));
            });
        }

        private static void AddApiCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Default", policy => policy
                    .WithOrigins("http://example.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });
        }
    }
}