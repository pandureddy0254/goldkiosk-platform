using System.Net;
using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Audit;

/// <summary>
/// Maps onto <c>audit.audit_events</c>. Append-only audit log. The DB has a
/// BEFORE UPDATE/DELETE trigger (<c>db/0201</c>) that raises an exception on
/// any attempt to modify a row — services must only INSERT.
/// Does NOT implement <see cref="IAuditableEntity"/> by design.
/// </summary>
public sealed class AuditEvent : ITenantScoped
{
    /// <summary>Gets or sets the sequence no.</summary>
    public long SequenceNo { get; set; }
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the actor type.</summary>
    public string ActorType { get; set; } = "system";  // user | kiosk | system | service
    /// <summary>Gets or sets the actor user id.</summary>
    public Guid? ActorUserId { get; set; }
    /// <summary>Gets or sets the actor label.</summary>
    public string? ActorLabel { get; set; }

    /// <summary>Gets or sets the log type.</summary>
    public string LogType { get; set; } = "INFO";       // INFO | WARN | ERROR | SECURITY
    /// <summary>Gets or sets the activity.</summary>
    public string Activity { get; set; } = string.Empty;
    /// <summary>Gets or sets the module.</summary>
    public string? Module { get; set; }
    /// <summary>Gets or sets the sub module.</summary>
    public string? SubModule { get; set; }
    /// <summary>Gets or sets the sub sub module.</summary>
    public string? SubSubModule { get; set; }

    /// <summary>Gets or sets the target type.</summary>
    public string? TargetType { get; set; }
    /// <summary>Gets or sets the target id.</summary>
    public Guid? TargetId { get; set; }

    /// <summary>Raw jsonb payload. Stored as <c>jsonb</c>, exposed as a string for simplicity.</summary>
    public string? BeforeJson { get; set; }
    /// <summary>Gets or sets the after json.</summary>
    public string? AfterJson { get; set; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>Gets or sets the source IP.</summary>
    public IPAddress? SourceIp { get; set; }

    /// <summary>Gets or sets the occurred at.</summary>
    public DateTimeOffset OccurredAt { get; set; }
}
