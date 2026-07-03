using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Kiosk location configuration.</summary>
public sealed class KioskLocationConfiguration : IEntityTypeConfiguration<KioskLocation>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<KioskLocation> builder)
    {
        builder.ToTable("kiosk_locations", "kiosk");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.City).IsRequired();
        builder.Property(x => x.Latitude).HasColumnType("numeric(9,6)");
        builder.Property(x => x.Longitude).HasColumnType("numeric(9,6)");

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_kiosk_locations__tenant_code")
            .IsUnique();
    }
}
