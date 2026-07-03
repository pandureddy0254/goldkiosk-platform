using GoldKiosk.Infrastructure.Entities.Voucher;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Voucher;

/// <summary>Promotional offer configuration.</summary>
public sealed class PromotionalOfferConfiguration : IEntityTypeConfiguration<PromotionalOffer>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<PromotionalOffer> builder)
    {
        builder.ToTable("promotional_offers", "voucher");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Description).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_promotional_offers__tenant_code").IsUnique();

        // Shadow FK to users (created_by_user_id).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
