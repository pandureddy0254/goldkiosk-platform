using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i kiosk inventory monitoring service.</summary>
/// <summary>I kiosk inventory monitoring service.</summary>
public interface IKioskInventoryMonitoringService
{
    /// <summary>List.</summary>
    Task<KioskInventorySnapshotList> ListAsync(
        string? search,
        string? metal,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);
}
