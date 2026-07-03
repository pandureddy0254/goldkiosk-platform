using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Maintenance ticket configuration.</summary>
public sealed class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.ToTable("maintenance_tickets", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.TicketType).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Priority).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_maintenance_tickets__tenant_code")
            .IsUnique();

        builder.HasIndex(x => new { x.KioskId, x.Status })
            .HasDatabaseName("ix_maintenance_tickets__kiosk");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Technician>()
            .WithMany()
            .HasForeignKey(x => x.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
