using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>System health snapshot configuration.</summary>
public sealed class SystemHealthSnapshotConfiguration : IEntityTypeConfiguration<SystemHealthSnapshot>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<SystemHealthSnapshot> builder)
    {
        builder.ToTable("system_health_snapshots", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AvgUptimePct).HasColumnType("numeric");

        builder.HasIndex(x => new { x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_system_health_snapshots__tenant_time")
            .IsDescending(false, true);
    }
}
