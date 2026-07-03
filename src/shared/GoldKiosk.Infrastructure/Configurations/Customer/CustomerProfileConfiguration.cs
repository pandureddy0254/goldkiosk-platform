using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer profile configuration.</summary>
public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("customer_profiles", "customer");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.CustomerId)
            .HasDatabaseName("uq_customer_profiles__customer").IsUnique();

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
