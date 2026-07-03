using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Wallet ledger entry configuration.</summary>
public sealed class WalletLedgerEntryConfiguration : IEntityTypeConfiguration<WalletLedgerEntry>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<WalletLedgerEntry> builder)
    {
        builder.ToTable("wallet_ledger_entries", "customer");
        // Composite single-col PK is sequence_no; id is uuid-uniquified.
        builder.HasKey(e => e.SequenceNo);
        builder.Property(e => e.SequenceNo).ValueGeneratedOnAdd();
        builder.HasIndex(e => e.Id).HasDatabaseName("uq_wallet_ledger_entries__id").IsUnique();

        builder.Property(e => e.EntryKind).IsRequired();

        builder.HasOne<CustomerWallet>()
            .WithMany()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
