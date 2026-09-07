using ECommerce.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ECommerce.Extensions
{
    public static class ApplicationPipelineExtensions
    {
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            // devolopment environment
            app.UseOpenApi();

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
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