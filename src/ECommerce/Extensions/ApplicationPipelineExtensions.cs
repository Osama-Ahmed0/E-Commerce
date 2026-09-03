using ECommerce.Middleware;

namespace ECommerce.Extensions
{
    public static class ApplicationPipelineExtensions
    {
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseCors("Default");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseMiddleware<ApiKeyMiddleware>();
            app.UseAuthorization();
            app.UseOutputCache();
            app.MapControllers();

            return app;
        }
    }
}
