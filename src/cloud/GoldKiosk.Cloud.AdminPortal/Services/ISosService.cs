using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i SOS service.</summary>
/// <summary>I SOS service.</summary>
public interface ISosService
{
    /// <summary>List.</summary>
    Task<SosRequestList> ListAsync(string? status, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>Acknowledge.</summary>
    Task<OperationResult> AcknowledgeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Dispatch.</summary>
    Task<OperationResult> DispatchAsync(Guid id, Guid technicianId, CancellationToken ct = default);

    /// <summary>Resolve.</summary>
    Task<OperationResult> ResolveAsync(Guid id, CancellationToken ct = default);
}
