using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i feedback service.</summary>
/// <summary>I feedback service.</summary>
public interface IFeedbackService
{
    /// <summary>List.</summary>
    Task<FeedbackMasterVM> ListAsync(
        string? search,
        string? stat,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Mark as read.</summary>
    Task<OperationResult> MarkAsReadAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mark as unread.</summary>
    Task<OperationResult> MarkAsUnreadAsync(Guid id, CancellationToken ct = default);

    /// <summary>Export csv.</summary>
    Task<byte[]> ExportCsvAsync(
        string? search,
        string? stat,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);
}
