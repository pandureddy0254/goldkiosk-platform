using GoldKiosk.Infrastructure.Entities.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Tx;

/// <summary>Transaction item configuration.</summary>
public sealed class TransactionItemConfiguration : IEntityTypeConfiguration<TransactionItem>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TransactionItem> builder)
    {
        builder.ToTable("transaction_items", "tx");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemType).IsRequired();
        builder.Property(i => i.Metal).IsRequired();

        builder.HasIndex(i => new { i.TransactionId, i.ItemIndex })
            .HasDatabaseName("uq_transaction_items__transaction_index").IsUnique();

        // Shadow FK so SaveChanges orders inserts (transactions → transaction_items).
        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(i => i.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
