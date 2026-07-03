using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i role management service.</summary>
/// <summary>I role management service.</summary>
public interface IRoleManagementService
{
    /// <summary>List.</summary>
    Task<RoleMasterList> ListAsync(string? search, string? stat, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Get by code.</summary>
    Task<RoleMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(RoleMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(RoleMasterViewModel vm, CancellationToken ct = default);

    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default);
}
