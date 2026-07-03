using GoldKiosk.Infrastructure.Entities.Merchant;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Merchant;

/// <summary>Wallet topup request configuration.</summary>
public sealed class WalletTopupRequestConfiguration : IEntityTypeConfiguration<WalletTopupRequest>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<WalletTopupRequest> builder)
    {
        builder.ToTable("wallet_topup_requests", "merchant");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CurrencyCode).IsRequired();

        // Shadow FK so SaveChanges orders inserts (franchise_wallets → wallet_topup_requests).
        builder.HasOne<FranchiseWallet>()
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Shadow FK to users (approved_by_user_id).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
