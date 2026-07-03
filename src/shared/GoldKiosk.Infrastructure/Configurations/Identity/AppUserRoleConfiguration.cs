using GoldKiosk.Infrastructure.Entities.Identity;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App user role configuration.</summary>
public sealed class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppUserRole> builder)
    {
        builder.ToTable("user_roles", "identity");
        builder.HasKey(ur => ur.Id);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Shadow FK so SaveChanges orders inserts (users → user_roles).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ur => ur.UserId).HasDatabaseName("ix_user_roles__user")
            .HasFilter("revoked_at IS NULL");
    }
}
