using GoldKiosk.Cloud.AdminPortal.Models;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Read-only service for the nine reports under <c>Views/Reports/*</c>.
/// All methods are tenant-scoped via <c>currentUser.TenantId</c>.
/// </summary>
public interface IReportsService
{
    /// <summary>Daily sales.</summary>
    Task<DailySalesReportViewModel> DailySalesAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Holding sales.</summary>
    Task<HoldingSalesReportViewModel> HoldingSalesAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Carat weight.</summary>
    Task<CaratWeightReportViewModel> CaratWeightAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Total expense.</summary>
    Task<TotalExpenseReportViewModel> TotalExpenseAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Worth.</summary>
    Task<WorthReportViewModel> WorthAsync(DateTime? asOf, CancellationToken ct = default);
    /// <summary>Sales payout.</summary>
    Task<SalesPayoutReportViewModel> SalesPayoutAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Expected profit.</summary>
    Task<ExpectedProfitReportViewModel> ExpectedProfitAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Profit after expense.</summary>
    Task<ProfitAfterExpenseReportViewModel> ProfitAfterExpenseAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    /// <summary>Offers.</summary>
    Task<OfferReportViewModel> OffersAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
}
