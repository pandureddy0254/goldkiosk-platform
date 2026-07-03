using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i audit log service.</summary>
/// <summary>I audit log service.</summary>
public interface IAuditLogService
{
    /// <summary>List.</summary>
    Task<UserActivityList> ListAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? category,
        string? subCategory,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Export csv.</summary>
    Task<byte[]> ExportCsvAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? category,
        string? subCategory,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);
}
