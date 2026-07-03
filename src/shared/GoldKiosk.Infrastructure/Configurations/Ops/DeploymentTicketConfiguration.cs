using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Deployment ticket configuration.</summary>
public sealed class DeploymentTicketConfiguration : IEntityTypeConfiguration<DeploymentTicket>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<DeploymentTicket> builder)
    {
        builder.ToTable("deployment_tickets", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Priority).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_deployment_tickets__tenant_code")
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_deployment_tickets__status");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Technician>()
            .WithMany()
            .HasForeignKey(x => x.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
