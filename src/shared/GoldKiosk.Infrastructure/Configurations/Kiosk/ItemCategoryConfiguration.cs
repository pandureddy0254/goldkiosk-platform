using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Item category configuration.</summary>
public sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ToTable("item_categories", "kiosk");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CategoryKey).IsRequired();
        builder.Property(c => c.DisplayName).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.CategoryKey })
            .HasDatabaseName("uq_item_categories__tenant_key").IsUnique();
    }
}
