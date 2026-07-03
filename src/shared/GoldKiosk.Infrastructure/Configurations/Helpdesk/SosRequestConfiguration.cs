using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Helpdesk;

/// <summary>SOS request configuration.</summary>
public sealed class SosRequestConfiguration : IEntityTypeConfiguration<SosRequest>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<SosRequest> builder)
    {
        builder.ToTable("sos_requests", "helpdesk");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => x.RaisedAt)
            .HasDatabaseName("ix_sos_requests__open")
            .HasFilter("status IN ('raised','acknowledged','dispatched')");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Ops.Technician>()
            .WithMany()
            .HasForeignKey(x => x.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
