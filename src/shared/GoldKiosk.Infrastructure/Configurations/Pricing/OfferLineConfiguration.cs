using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Pricing;

/// <summary>Offer line configuration.</summary>
public sealed class OfferLineConfiguration : IEntityTypeConfiguration<OfferLine>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<OfferLine> builder)
    {
        builder.ToTable("offer_lines", "pricing");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OfferId).HasDatabaseName("ix_offer_lines__offer");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<Offer>()
            .WithMany()
            .HasForeignKey(x => x.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Entities.Tx.TransactionItem>()
            .WithMany()
            .HasForeignKey(x => x.TransactionItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MetalRate>()
            .WithMany()
            .HasForeignKey(x => x.MetalRateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
