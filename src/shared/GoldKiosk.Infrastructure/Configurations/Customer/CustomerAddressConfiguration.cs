using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer address configuration.</summary>
public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses", "customer");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.City).IsRequired();
        builder.Property(a => a.CountryCode).HasColumnType("char(2)").IsRequired();

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
