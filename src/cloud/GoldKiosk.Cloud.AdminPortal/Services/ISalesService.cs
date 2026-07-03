using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>I sales service.</summary>
public interface ISalesService
{
    /// <summary>List transactions for the Sales pages. <paramref name="kind"/>
    /// is one of <c>"precious_sale"</c> or <c>"pawn"</c>.</summary>
    Task<SalesClientList> ListAsync(
        string kind,
        string? search,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        bool showJunkOnly,
        bool showActiveOnly,
        int pageSize,
        int pageNo,
        CancellationToken ct = default);

    /// <summary>Export csv.</summary>
    Task<byte[]> ExportCsvAsync(
        string kind,
        string? search,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        bool showJunkOnly,
        bool showActiveOnly,
        CancellationToken ct = default);
}
