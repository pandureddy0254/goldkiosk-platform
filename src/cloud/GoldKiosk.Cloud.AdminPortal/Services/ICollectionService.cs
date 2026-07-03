using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i collection service.</summary>
/// <summary>I collection service.</summary>
public interface ICollectionService
{
    /// <summary>List.</summary>
    Task<CollectionTicketList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default);
    /// <summary>Add run.</summary>
    Task<OperationResult> AddRunAsync(CollectionRunViewModel vm, CancellationToken ct = default);
    /// <summary>Edit run.</summary>
    Task<OperationResult> EditRunAsync(CollectionRunViewModel vm, CancellationToken ct = default);
    /// <summary>Delete run.</summary>
    Task<OperationResult> DeleteRunAsync(Guid id, CancellationToken ct = default);
    /// <summary>Add ticket.</summary>
    Task<OperationResult> AddTicketAsync(CollectionTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Edit ticket.</summary>
    Task<OperationResult> EditTicketAsync(CollectionTicketViewModel vm, CancellationToken ct = default);
    /// <summary>Sign off.</summary>
    Task<OperationResult> SignOffAsync(Guid ticketId, CancellationToken ct = default);
}
