using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Aggregates tenant-scoped summary data for the Home landing page.
/// </summary>
public interface IDashboardService
{
    /// <summary>Get.</summary>
    Task<DashboardViewModel> GetAsync(CancellationToken ct = default);
}
