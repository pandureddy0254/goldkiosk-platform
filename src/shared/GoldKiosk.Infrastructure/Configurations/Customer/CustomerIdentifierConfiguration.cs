using GoldKiosk.Infrastructure.Entities.Customer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Customer;

/// <summary>Customer identifier configuration.</summary>
public sealed class CustomerIdentifierConfiguration : IEntityTypeConfiguration<CustomerIdentifier>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CustomerIdentifier> builder)
    {
        builder.ToTable("customer_identifiers", "customer");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IdType).IsRequired();
        builder.Property(i => i.IssuingCountry).HasColumnType("char(2)").IsRequired();

        builder.HasQueryFilter(i => i.DeletedAt == null);

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
