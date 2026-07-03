using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Technician cluster configuration.</summary>
public sealed class TechnicianClusterConfiguration : IEntityTypeConfiguration<TechnicianCluster>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TechnicianCluster> builder)
    {
        builder.ToTable("technician_clusters", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Name).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_technician_clusters__tenant_code")
            .IsUnique();
    }
}
