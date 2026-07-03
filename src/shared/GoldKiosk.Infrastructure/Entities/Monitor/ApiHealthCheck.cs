namespace GoldKiosk.Infrastructure.Entities.Monitor;

/// <summary>
/// Maps onto <c>monitor.api_health_checks</c>. Global health-probe results
/// (no tenant scoping — these are platform observability data points).
/// </summary>
public sealed class ApiHealthCheck
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the endpoint.</summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the checked at.</summary>
    public DateTimeOffset CheckedAt { get; set; }
    /// <summary>Gets or sets the response time ms.</summary>
    public int ResponseTimeMs { get; set; }
    /// <summary>Gets or sets the status code.</summary>
    public int StatusCode { get; set; }
    /// <summary>Gets or sets a value indicating whether is healthy.</summary>
    public bool IsHealthy { get; set; }
    /// <summary>Gets or sets the region.</summary>
    public string? Region { get; set; }
}
