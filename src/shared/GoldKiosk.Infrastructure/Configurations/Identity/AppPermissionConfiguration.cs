using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App permission configuration.</summary>
public sealed class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppPermission> builder)
    {
        builder.ToTable("permissions", "identity");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasColumnType("citext").IsRequired();
        builder.Property(p => p.DisplayName).IsRequired();
        builder.Property(p => p.Scope).IsRequired();

        builder.HasIndex(p => p.Code).HasDatabaseName("uq_permissions__code").IsUnique();
    }
}
