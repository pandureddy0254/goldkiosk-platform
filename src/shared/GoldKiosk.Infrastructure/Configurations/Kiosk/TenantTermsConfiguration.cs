using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Tenant terms configuration.</summary>
public sealed class TenantTermsConfiguration : IEntityTypeConfiguration<TenantTerms>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TenantTerms> builder)
    {
        builder.ToTable("tenant_terms", "kiosk");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Version).IsRequired();
        builder.Property(t => t.Title).IsRequired();
        builder.Property(t => t.SectionsJson).HasColumnType("jsonb").IsRequired();

        // Uniqueness enforced by partial indexes in the DB (0805 script) — one for
        // tenant-specific rows, one for global defaults (tenant_id IS NULL).
        builder.Property(t => t.TenantId).IsRequired(false);
    }
}
