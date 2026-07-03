using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Screen saver configuration.</summary>
public sealed class ScreenSaverConfiguration : IEntityTypeConfiguration<ScreenSaver>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<ScreenSaver> builder)
    {
        builder.ToTable("screen_savers", "kiosk");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).HasColumnType("citext").IsRequired();
        builder.Property(s => s.ImageUrl).IsRequired();
        builder.Property(s => s.MediaType).IsRequired();

        // Uniqueness enforced by partial indexes in the DB (0804 script) — one for
        // tenant-specific rows, one for global defaults (tenant_id IS NULL).
        builder.Property(s => s.TenantId).IsRequired(false);
    }
}
