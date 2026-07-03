using GoldKiosk.Infrastructure.Entities.Tx;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Tx;

/// <summary>Transaction configuration.</summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", "tx");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TransactionCode).HasColumnType("citext").IsRequired();
        builder.Property(t => t.Kind).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.CurrencyCode).HasColumnType("char(3)").IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.TransactionCode })
            .HasDatabaseName("uq_transactions__tenant_code").IsUnique();
        builder.HasIndex(t => t.CustomerId).HasDatabaseName("ix_transactions__customer");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(t => t.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.CoordinatorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
