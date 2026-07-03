using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer wallet configuration.</summary>
public sealed class CustomerWalletConfiguration : IEntityTypeConfiguration<CustomerWallet>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CustomerWallet> builder)
    {
        builder.ToTable("customer_wallets", "customer");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CurrencyCode).HasColumnType("char(3)").IsRequired();
        builder.Property(w => w.Status).IsRequired();
        builder.Property(w => w.PinHash).IsRequired();

        builder.HasIndex(w => new { w.CustomerId, w.CurrencyCode })
            .HasDatabaseName("uq_customer_wallets__customer_currency").IsUnique();

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
