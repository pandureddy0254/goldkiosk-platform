using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Kiosk configuration.</summary>
public sealed class KioskConfiguration : IEntityTypeConfiguration<Entities.Kiosk.Kiosk>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Entities.Kiosk.Kiosk> builder)
    {
        builder.ToTable("kiosks", "kiosk");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Code).HasColumnType("citext").IsRequired();
        builder.Property(k => k.FriendlyName).IsRequired();
        builder.Property(k => k.Status).IsRequired();
        builder.Property(k => k.HardwareModel).IsRequired();
        builder.Property(k => k.MsixChannel).IsRequired();

        builder.HasIndex(k => new { k.TenantId, k.Code })
            .HasDatabaseName("uq_kiosks__tenant_code").IsUnique();

        builder.HasIndex(k => new { k.TenantId, k.Status })
            .HasDatabaseName("ix_kiosks__tenant_status")
            .HasFilter("is_active = true");

        // Shadow FK so SaveChanges orders inserts (kiosk_locations → kiosks).
        builder.HasOne<KioskLocation>()
            .WithMany()
            .HasForeignKey(k => k.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
