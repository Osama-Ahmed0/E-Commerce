using ECommerce.Mapping;
using ECommerce.Services;
using ECommerce.Services.Validation;

namespace ECommerce.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICategoryValidator, CategoryValidator>();

            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductValidator, ProductValidator>();

            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICartValidator, CartValidator>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderValidator, OrderValidator>();

            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IReviewValidator, ReviewValidator>();

            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

            return services;
        }
    }
}