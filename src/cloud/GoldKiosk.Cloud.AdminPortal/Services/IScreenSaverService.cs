using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i screen saver service.</summary>
/// <summary>I screen saver service.</summary>
public interface IScreenSaverService
{
    /// <summary>List.</summary>
    Task<ScreenSaverMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(ScreenSaverMasterViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(ScreenSaverMasterViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default);
}
