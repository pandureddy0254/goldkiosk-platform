using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i system health service.</summary>
/// <summary>I system health service.</summary>
public interface ISystemHealthService
{
    /// <summary>Get dashboard.</summary>
    Task<SystemHealthDashboardViewModel> GetDashboardAsync(CancellationToken ct = default);
}
