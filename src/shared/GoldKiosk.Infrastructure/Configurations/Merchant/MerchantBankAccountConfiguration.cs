using GoldKiosk.Infrastructure.Entities.Merchant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Merchant;

/// <summary>Merchant bank account configuration.</summary>
public sealed class MerchantBankAccountConfiguration : IEntityTypeConfiguration<MerchantBankAccount>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<MerchantBankAccount> builder)
    {
        builder.ToTable("merchant_bank_accounts", "merchant");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HolderNameEnc).IsRequired();
        builder.Property(x => x.CurrencyCode).IsRequired();

        // Shadow FK so SaveChanges orders inserts (merchants → merchant_bank_accounts).
        builder.HasOne<Entities.Merchant.Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
