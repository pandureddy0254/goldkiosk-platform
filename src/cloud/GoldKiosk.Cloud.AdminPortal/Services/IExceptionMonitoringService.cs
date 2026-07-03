using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
// OperationResult lives in Services.Common.

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i exception monitoring service.</summary>
/// <summary>I exception monitoring service.</summary>
public interface IExceptionMonitoringService
{
    /// <summary>List.</summary>
    Task<ExceptionLogList> ListAsync(
        string? search,
        string? severity,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Mark resolved.</summary>
    Task<OperationResult> MarkResolvedAsync(Guid id, CancellationToken ct = default);
}
