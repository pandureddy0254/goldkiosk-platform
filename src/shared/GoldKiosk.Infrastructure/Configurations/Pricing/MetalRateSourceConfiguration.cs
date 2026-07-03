using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Pricing;

/// <summary>Metal rate source configuration.</summary>
public sealed class MetalRateSourceConfiguration : IEntityTypeConfiguration<MetalRateSource>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<MetalRateSource> builder)
    {
        builder.ToTable("metal_rate_sources", "pricing");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.AdapterType).IsRequired();

        builder.HasIndex(x => x.Code).HasDatabaseName("uq_metal_rate_sources__code").IsUnique();
    }
}
