using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i access control service.</summary>
/// <summary>I access control service.</summary>
public interface IAccessControlService
{
    /// <summary>List.</summary>
    Task<AccessControlPageViewModel> ListAsync(string? roleCode, string? search, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Update.</summary>
    Task<OperationResult> UpdateAsync(AccessControlPageViewModel vm, CancellationToken ct = default);

    /// <summary>Update single.</summary>
    Task<OperationResult> UpdateSingleAsync(AccessModuleViewModel vm, CancellationToken ct = default);
}
