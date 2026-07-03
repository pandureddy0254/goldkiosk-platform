using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i API monitoring service.</summary>
/// <summary>I API monitoring service.</summary>
public interface IApiMonitoringService
{
    /// <summary>Get stats.</summary>
    Task<ApiHealthStatsViewModel> GetStatsAsync(
        string? search,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);
}
