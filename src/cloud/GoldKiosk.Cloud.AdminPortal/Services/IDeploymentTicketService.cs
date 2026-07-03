using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i deployment ticket service.</summary>
/// <summary>I deployment ticket service.</summary>
public interface IDeploymentTicketService
{
    /// <summary>List.</summary>
    Task<DeploymentTicketList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(DeploymentTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(DeploymentTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    /// <summary>Change status.</summary>
    Task<OperationResult> ChangeStatusAsync(Guid id, string newStatus, string? remarks, CancellationToken ct = default);
}
