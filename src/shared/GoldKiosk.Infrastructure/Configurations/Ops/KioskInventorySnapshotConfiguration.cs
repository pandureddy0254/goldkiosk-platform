using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Kiosk inventory snapshot configuration.</summary>
public sealed class KioskInventorySnapshotConfiguration : IEntityTypeConfiguration<KioskInventorySnapshot>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<KioskInventorySnapshot> builder)
    {
        builder.ToTable("kiosk_inventory_snapshots", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Metal).IsRequired();
        builder.Property(x => x.WeightG).HasColumnType("numeric");
        builder.Property(x => x.Carat).HasColumnType("numeric(5,2)");

        builder.HasIndex(x => new { x.KioskId, x.CapturedAt })
            .HasDatabaseName("ix_kiosk_inventory_snapshots__kiosk_time")
            .IsDescending(false, true);

        // Shadow FK so SaveChanges orders inserts (kiosks → kiosk_inventory_snapshots).
        builder.HasOne<GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
