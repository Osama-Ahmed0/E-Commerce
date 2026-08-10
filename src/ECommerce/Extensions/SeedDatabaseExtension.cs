using ECommerce.Data;
using ECommerce.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Extensions
{
    public static class SeedDatabaseExtension
    {
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                await SeedRolesAsync(roleManager);

                var userManager = services.GetRequiredService<UserManager<User>>();
                await SeedAdminUserAsync(userManager, app.Configuration);

                var db = services.GetRequiredService<AppDbContext>();
                await DataSeeder.SeedAsync(db, "Data/SeedData/Data.json");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in Enum.GetNames<UserRole>())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<User> userManager, IConfiguration configuration)
        {
            var adminEmail = configuration["AdminSeed:Email"];

            if (await userManager.FindByEmailAsync(adminEmail!) == null)
            {
                var admin = new User
                {
                    FullName = configuration["AdminSeed:FullName"]!,
                    Email = adminEmail,
                    UserName = configuration["AdminSeed:UserName"]!,
                    Role = UserRole.Admin,
                };
                await userManager.CreateAsync(admin, configuration["AdminSeed:Password"]!);
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
