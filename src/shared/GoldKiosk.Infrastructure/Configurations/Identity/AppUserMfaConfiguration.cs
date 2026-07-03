using GoldKiosk.Infrastructure.Entities.Identity;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App user MFA configuration.</summary>
public sealed class AppUserMfaConfiguration : IEntityTypeConfiguration<AppUserMfa>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppUserMfa> builder)
    {
        builder.ToTable("user_mfa", "identity");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Method).IsRequired();

        // Shadow FK so SaveChanges orders inserts (users → user_mfa).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
