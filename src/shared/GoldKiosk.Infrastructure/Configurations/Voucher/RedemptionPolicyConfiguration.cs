using GoldKiosk.Infrastructure.Entities.Voucher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Voucher;

/// <summary>Redemption policy configuration.</summary>
public sealed class RedemptionPolicyConfiguration : IEntityTypeConfiguration<RedemptionPolicy>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<RedemptionPolicy> builder)
    {
        builder.ToTable("redemption_policies", "voucher");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicableCategories).HasColumnType("jsonb");

        // Shadow FK so SaveChanges orders inserts (vouchers → redemption_policies).
        builder.HasOne<Entities.Voucher.Voucher>()
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
