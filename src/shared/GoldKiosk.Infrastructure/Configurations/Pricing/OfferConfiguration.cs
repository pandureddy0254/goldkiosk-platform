using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Pricing;

/// <summary>Offer configuration.</summary>
public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers", "pricing");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();

        builder.HasIndex(x => x.TransactionId)
            .HasDatabaseName("uq_offers__transaction").IsUnique();

        // Shadow FK so SaveChanges orders inserts (transactions → offers).
        builder.HasOne<Entities.Tx.Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
