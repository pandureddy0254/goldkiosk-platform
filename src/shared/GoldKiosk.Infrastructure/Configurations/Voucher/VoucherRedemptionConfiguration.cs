using GoldKiosk.Infrastructure.Entities.Voucher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Voucher;

/// <summary>Voucher redemption configuration.</summary>
public sealed class VoucherRedemptionConfiguration : IEntityTypeConfiguration<VoucherRedemption>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.ToTable("voucher_redemptions", "voucher");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.VoucherId, x.RedeemedAt })
            .HasDatabaseName("ix_voucher_redemptions__voucher");
        builder.HasIndex(x => x.CustomerId)
            .HasDatabaseName("ix_voucher_redemptions__customer");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<Entities.Voucher.Voucher>()
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Tx.Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
