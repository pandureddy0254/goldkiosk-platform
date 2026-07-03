using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer configuration.</summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Entities.Customer.Customer>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Entities.Customer.Customer> builder)
    {
        builder.ToTable("customers", "customer");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode).HasColumnType("citext").IsRequired();
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.PrimaryNationality).HasColumnType("char(2)");

        builder.HasIndex(c => new { c.TenantId, c.CustomerCode })
            .HasDatabaseName("uq_customers__tenant_code").IsUnique();

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
