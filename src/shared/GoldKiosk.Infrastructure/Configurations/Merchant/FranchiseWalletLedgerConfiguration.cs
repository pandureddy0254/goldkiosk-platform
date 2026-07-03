using GoldKiosk.Infrastructure.Entities.Merchant;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Merchant;

/// <summary>Franchise wallet ledger configuration.</summary>
public sealed class FranchiseWalletLedgerConfiguration : IEntityTypeConfiguration<FranchiseWalletLedger>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<FranchiseWalletLedger> builder)
    {
        builder.ToTable("franchise_wallet_ledger", "merchant");
        builder.HasKey(x => x.SequenceNo);

        builder.Property(x => x.SequenceNo)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.EntryKind).IsRequired();

        builder.HasIndex(x => x.Id).IsUnique().HasDatabaseName("uq_franchise_wallet_ledger__id");
        builder.HasIndex(x => new { x.WalletId, x.OccurredAt })
            .HasDatabaseName("ix_franchise_wallet_ledger__wallet");

        // Shadow FK so SaveChanges orders inserts (franchise_wallets → franchise_wallet_ledger).
        builder.HasOne<FranchiseWallet>()
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Shadow FK to users (posted_by_user_id).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.PostedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
