using GoldKiosk.Infrastructure.Entities.Merchant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Merchant;

/// <summary>Franchise wallet configuration.</summary>
public sealed class FranchiseWalletConfiguration : IEntityTypeConfiguration<FranchiseWallet>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<FranchiseWallet> builder)
    {
        builder.ToTable("franchise_wallets", "merchant");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CurrencyCode).IsRequired();

        builder.HasIndex(x => new { x.MerchantId, x.CurrencyCode })
            .HasDatabaseName("uq_franchise_wallets__merchant_currency").IsUnique();

        // Shadow FK so SaveChanges orders inserts (merchants → franchise_wallets).
        builder.HasOne<Entities.Merchant.Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
