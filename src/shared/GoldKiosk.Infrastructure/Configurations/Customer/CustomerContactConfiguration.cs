using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer contact configuration.</summary>
public sealed class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("customer_contacts", "customer");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Channel).IsRequired();

        // Declare the FK so SaveChanges orders inserts (customers → customer_contacts).
        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
