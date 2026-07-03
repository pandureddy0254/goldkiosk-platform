using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Voucher;

/// <summary>Voucher configuration.</summary>
public sealed class VoucherConfiguration : IEntityTypeConfiguration<Entities.Voucher.Voucher>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Entities.Voucher.Voucher> builder)
    {
        builder.ToTable("vouchers", "voucher");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code).HasColumnType("citext").IsRequired();
        builder.Property(v => v.Name).IsRequired();
        builder.Property(v => v.DiscountType).IsRequired();

        builder.HasIndex(v => new { v.TenantId, v.Code })
            .HasDatabaseName("uq_vouchers__tenant_code").IsUnique();
    }
}
