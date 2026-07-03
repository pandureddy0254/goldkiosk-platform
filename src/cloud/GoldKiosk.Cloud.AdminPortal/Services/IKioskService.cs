using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i kiosk service.</summary>
/// <summary>I kiosk service.</summary>
public interface IKioskService
{
    /// <summary>List.</summary>
    Task<KioskMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Get by code.</summary>
    Task<KioskMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(KioskMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(KioskMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default);
}
