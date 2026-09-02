using ECommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Test
{
    internal class InMemoryDbContext() : AppDbContext(CreateInMemoryOptions())
    {
        private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        public override void Dispose()
        {
            Database.EnsureDeleted();
            base.Dispose();
        }
    }
}