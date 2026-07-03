using GoldKiosk.Infrastructure.Entities.Monitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Monitor;

/// <summary>API request log configuration.</summary>
public sealed class ApiRequestLogConfiguration : IEntityTypeConfiguration<ApiRequestLog>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<ApiRequestLog> builder)
    {
        builder.ToTable("api_request_logs", "monitor");

        // Composite PK aligns with the partitioned-table constraint
        // pk_api_request_logs (sequence_no, requested_at).
        builder.HasKey(x => new { x.SequenceNo, x.RequestedAt })
            .HasName("pk_api_request_logs");

        builder.Property(x => x.SequenceNo)
            .ValueGeneratedOnAdd()
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Endpoint).IsRequired();

        builder.HasIndex(x => new { x.Endpoint, x.RequestedAt })
            .HasDatabaseName("ix_api_request_logs__endpoint_time")
            .IsDescending(false, true);
    }
}
