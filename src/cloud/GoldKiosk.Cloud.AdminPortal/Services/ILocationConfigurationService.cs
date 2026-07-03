using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i location configuration service.</summary>
/// <summary>I location configuration service.</summary>
public interface ILocationConfigurationService
{
    /// <summary>List.</summary>
    Task<KioskLocationList> ListAsync(string? search, string? stat, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(KioskLocationViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(KioskLocationViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
