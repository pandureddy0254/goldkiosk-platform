using GoldKiosk.Infrastructure.Entities.Monitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Monitor;

/// <summary>API health check configuration.</summary>
public sealed class ApiHealthCheckConfiguration : IEntityTypeConfiguration<ApiHealthCheck>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<ApiHealthCheck> builder)
    {
        builder.ToTable("api_health_checks", "monitor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Endpoint).IsRequired();

        builder.HasIndex(x => new { x.Endpoint, x.CheckedAt })
            .HasDatabaseName("ix_api_health_checks__endpoint_time")
            .IsDescending(false, true);
    }
}
