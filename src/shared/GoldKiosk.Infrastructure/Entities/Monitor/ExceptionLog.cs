namespace GoldKiosk.Infrastructure.Entities.Monitor;

/// <summary>
/// Maps onto <c>monitor.exception_logs</c>. Captures unhandled exceptions from
/// the platform — kiosk client, admin dashboard, API gateway, jobs, integrations.
/// <para>
/// <b>Tenancy:</b> <c>tenant_id</c> is nullable because platform-level errors
/// (e.g. failures before a request is bound to a tenant) have no tenant. The
/// entity therefore does <b>not</b> implement <see cref="Common.ITenantScoped"/>
/// — the contract requires a non-nullable <c>Guid TenantId</c>. Tenant filtering
/// is performed manually in <c>ExceptionMonitoringService</c>: a tenant user sees
/// their own rows plus platform-null rows; a platform admin (no current tenant)
/// sees all rows.
/// </para>
/// </summary>
public sealed class ExceptionLog
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the user id.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the source.</summary>
    public string Source { get; set; } = "api";          // kiosk | admin | api | job | integration
    /// <summary>Gets or sets the exception name.</summary>
    public string ExceptionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the stack trace.</summary>
    public string? StackTrace { get; set; }
    /// <summary>Gets or sets the severity.</summary>
    public string Severity { get; set; } = "error";      // info | warn | error | critical

    /// <summary>Gets or sets the occurred at.</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Gets or sets a value indicating whether is resolved.</summary>
    public bool IsResolved { get; set; }
    /// <summary>Gets or sets the resolved by user id.</summary>
    public Guid? ResolvedByUserId { get; set; }
    /// <summary>Gets or sets the resolved at.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
}
