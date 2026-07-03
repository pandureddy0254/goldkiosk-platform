using GoldKiosk.Infrastructure.Entities.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Tx;

/// <summary>Payout configuration.</summary>
public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts", "payment");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).IsRequired();
    }
}
