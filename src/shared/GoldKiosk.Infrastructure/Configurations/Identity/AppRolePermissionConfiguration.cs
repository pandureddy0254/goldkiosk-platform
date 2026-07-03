using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App role permission configuration.</summary>
public sealed class AppRolePermissionConfiguration : IEntityTypeConfiguration<AppRolePermission>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppRolePermission> builder)
    {
        builder.ToTable("role_permissions", "identity");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId }).HasName("pk_role_permissions");

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
