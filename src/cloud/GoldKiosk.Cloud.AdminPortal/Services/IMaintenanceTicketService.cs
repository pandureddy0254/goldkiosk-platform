using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i maintenance ticket service.</summary>
/// <summary>I maintenance ticket service.</summary>
public interface IMaintenanceTicketService
{
    /// <summary>List.</summary>
    Task<MaintenanceTicketList> ListAsync(string? search, string? status, string? ticketType, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add.</summary>
    Task<OperationResult> AddAsync(MaintenanceTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Edit.</summary>
    Task<OperationResult> EditAsync(MaintenanceTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Delete.</summary>
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    /// <summary>Change status.</summary>
    Task<OperationResult> ChangeStatusAsync(Guid id, string newStatus, string? remarks, CancellationToken ct = default);
}
