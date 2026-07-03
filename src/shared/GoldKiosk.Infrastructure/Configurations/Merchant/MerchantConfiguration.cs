using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Merchant;

/// <summary>Merchant configuration.</summary>
public sealed class MerchantConfiguration : IEntityTypeConfiguration<Entities.Merchant.Merchant>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Entities.Merchant.Merchant> builder)
    {
        builder.ToTable("merchants", "merchant");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasColumnType("citext").IsRequired();
        builder.Property(m => m.Name).IsRequired();
        builder.Property(m => m.KycStatus).IsRequired();

        builder.HasIndex(m => new { m.TenantId, m.Code })
            .HasDatabaseName("uq_merchants__tenant_code").IsUnique();
    }
}
