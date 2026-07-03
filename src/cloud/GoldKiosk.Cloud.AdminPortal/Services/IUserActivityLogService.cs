using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i user activity log service.</summary>
/// <summary>I user activity log service.</summary>
public interface IUserActivityLogService
{
    /// <summary>List.</summary>
    Task<UserActivityList> ListAsync(
        string? search,
        string? logType,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);
}
