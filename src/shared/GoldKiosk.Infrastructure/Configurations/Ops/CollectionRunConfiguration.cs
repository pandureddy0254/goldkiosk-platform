using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Collection run configuration.</summary>
public sealed class CollectionRunConfiguration : IEntityTypeConfiguration<CollectionRun>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CollectionRun> builder)
    {
        builder.ToTable("collection_runs", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("numeric");
        builder.Property(x => x.RunDate).HasColumnType("date");

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_collection_runs__tenant_code")
            .IsUnique();

        // Shadow FK to technicians (lead_technician_id).
        builder.HasOne<Technician>()
            .WithMany()
            .HasForeignKey(x => x.LeadTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
