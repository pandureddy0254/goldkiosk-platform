namespace GoldKiosk.Infrastructure.Entities.Monitor;

/// <summary>
/// Maps onto <c>monitor.api_request_logs</c>. Append-only and <b>partitioned by
/// <c>requested_at</c></b>; the composite primary key is
/// <c>(sequence_no, requested_at)</c>. Inserts come from the API/gateway
/// pipeline — the dashboard only reads. Reads against partitioned tables work
/// transparently in EF Core.
/// </summary>
public sealed class ApiRequestLog
{
    /// <summary>Gets or sets the sequence no.</summary>
    public long SequenceNo { get; set; }
    /// <summary>Gets or sets the requested at.</summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Gets or sets the endpoint.</summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the response time ms.</summary>
    public int ResponseTimeMs { get; set; }
    /// <summary>Gets or sets the status code.</summary>
    public int StatusCode { get; set; }
    /// <summary>Gets or sets a value indicating whether is success.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Gets or sets the caller kiosk id.</summary>
    public Guid? CallerKioskId { get; set; }
    /// <summary>Gets or sets the caller user id.</summary>
    public Guid? CallerUserId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
}
