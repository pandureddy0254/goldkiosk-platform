using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Pricing;

/// <summary>Metal rate configuration.</summary>
public sealed class MetalRateConfiguration : IEntityTypeConfiguration<MetalRate>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<MetalRate> builder)
    {
        builder.ToTable("metal_rates", "pricing");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Metal).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();

        builder.HasIndex(x => new { x.Metal, x.PurityKarat, x.CurrencyCode, x.RetrievedAt })
            .HasDatabaseName("ix_metal_rates__live");

        // Shadow FK so SaveChanges orders inserts (metal_rate_sources → metal_rates).
        builder.HasOne<MetalRateSource>()
            .WithMany()
            .HasForeignKey(x => x.MetalRateSourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
