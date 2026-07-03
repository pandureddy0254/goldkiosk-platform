using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App role configuration.</summary>
public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("roles", "identity");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasColumnType("citext").IsRequired();
        builder.Property(r => r.Name).IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.Code }).HasDatabaseName("uq_roles__tenant_code").IsUnique();
    }
}
