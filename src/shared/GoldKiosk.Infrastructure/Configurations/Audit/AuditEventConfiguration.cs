using GoldKiosk.Infrastructure.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Audit;

/// <summary>Audit event configuration.</summary>
public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events", "audit");

        // The DB primary key is (sequence_no); id has a unique constraint.
        builder.HasKey(x => x.SequenceNo);
        builder.Property(x => x.SequenceNo)
            .ValueGeneratedOnAdd()
            .UseIdentityAlwaysColumn();

        builder.HasAlternateKey(x => x.Id).HasName("uq_audit_events__id");
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ActorType).IsRequired();
        builder.Property(x => x.LogType).IsRequired();
        builder.Property(x => x.Activity).IsRequired();

        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");
        builder.Property(x => x.SourceIp).HasColumnType("inet");

        builder.HasIndex(x => new { x.TenantId, x.OccurredAt })
            .HasDatabaseName("ix_audit_events__tenant_occurred")
            .IsDescending(false, true);

        builder.HasIndex(x => x.Activity).HasDatabaseName("ix_audit_events__activity");
    }
}
