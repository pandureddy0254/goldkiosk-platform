using GoldKiosk.Infrastructure.Entities.Monitor;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// DbSets for the <c>monitor</c> schema (observability tables).
/// <para>
/// <c>api_request_logs</c> is partitioned by <c>requested_at</c>; EF reads work
/// transparently against the parent table. The dashboard does not insert here —
/// rows arrive from the API gateway pipeline.
/// </para>
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<ExceptionLog> ExceptionLogs => Set<ExceptionLog>();
    /// <summary>Set.</summary>
    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();
    /// <summary>Set.</summary>
    public DbSet<ApiHealthCheck> ApiHealthChecks => Set<ApiHealthCheck>();
}
