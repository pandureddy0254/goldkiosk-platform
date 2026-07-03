using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.system_health_snapshots</c>. Periodic rollup of fleet
/// health used by the <c>SystemHealthCheck</c> dashboard.
/// </summary>
public sealed class SystemHealthSnapshot : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the captured at.</summary>
    public DateTimeOffset CapturedAt { get; set; }
    /// <summary>Gets or sets the total cities.</summary>
    public int TotalCities { get; set; }
    /// <summary>Gets or sets the total kiosks.</summary>
    public int TotalKiosks { get; set; }
    /// <summary>Gets or sets the functional count.</summary>
    public int FunctionalCount { get; set; }
    /// <summary>Gets or sets the non functional count.</summary>
    public int NonFunctionalCount { get; set; }
    /// <summary>Gets or sets the avg uptime pct.</summary>
    public decimal AvgUptimePct { get; set; }
}
