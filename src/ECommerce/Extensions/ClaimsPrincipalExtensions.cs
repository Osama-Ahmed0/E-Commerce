using System.Security.Claims;

namespace ECommerce.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier);
        public static string? GetRole(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role);
    }
}
