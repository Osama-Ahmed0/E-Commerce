using ECommerce.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.DbConfiguration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");

        builder.OwnsMany(u => u.RefreshTokens, rt =>
        {
            rt.Property(r => r.Token).IsRequired().HasMaxLength(200);
            rt.Property(r => r.CreatedOn).IsRequired();
            rt.Property(r => r.ExpiresOn).IsRequired();
            rt.Property(r => r.RevokedOn);
        });
    }
}
