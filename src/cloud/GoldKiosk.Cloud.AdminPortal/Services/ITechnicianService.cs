using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i technician service.</summary>
/// <summary>I technician service.</summary>
public interface ITechnicianService
{
    /// <summary>List.</summary>
    Task<TechnicianList> ListAsync(string? search, Guid? clusterId, string? status, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(TechnicianViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(TechnicianViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    /// <summary>List clusters.</summary>
    Task<IReadOnlyList<TechnicianClusterViewModel>> ListClustersAsync(CancellationToken ct = default);
}
