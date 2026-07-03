using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Reports.Rows;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Read-only service backing the nine pages under <c>Views/Reports/</c>.
/// <para>
/// Most reports query the materialised views in <c>reporting.*</c>. Those
/// matviews are populated by <c>CALL reporting.refresh_all_matviews()</c>; on
/// a fresh database they exist but are empty (created <c>WITH NO DATA</c>),
/// and on a database that hasn't yet had <c>0401_matviews.sql</c> applied
/// they don't exist at all (SQLSTATE 42P01). Every method here handles both
/// cases gracefully so the page renders rather than 500s.
/// </para>
/// <para>
/// Column-alias contract: <c>SqlQuery&lt;T&gt;</c> matches result columns to
/// DTO properties via exact (case-insensitive) name match. EF Core's
/// snake_case naming convention only applies to mapped entities — NOT to
/// ad-hoc <c>SqlQuery</c> projections — so every SELECT here aliases each
/// column to its DTO property name with <c>AS "PropertyName"</c>.
/// </para>
/// </summary>
public sealed class ReportsService(
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<ReportsService> logger) : IReportsService
{
    /// <summary>
    /// PG SQLSTATE for "undefined_table". Thrown when a matview or table
    /// referenced by a SELECT doesn't exist. We treat this as "no data" for
    /// the reporting surface — the user shouldn't see a 500 on a brand-new
    /// install where the matview refresh hasn't been scheduled yet.
    /// </summary>
    private const string UndefinedTableSqlState = "42P01";
    private const string UnpopulatedMatviewSqlState = "55000"; // "object_not_in_prerequisite_state" — matview never refreshed.

    /// <summary>
    /// EF Core wraps the underlying Npgsql.PostgresException inside
    /// InvalidOperationException ("An exception has been raised that is
    /// likely due to a transient failure"). Walk the inner chain to find it.
    /// </summary>
    private static PostgresException? UnwrapPostgres(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg)
            {
                return pg;
            }
        }

        return null;
    }

    /// <summary>True if the exception is a "no data yet" condition the report should treat as empty.</summary>
    private static bool IsEmptyReportingException(Exception ex) =>
        UnwrapPostgres(ex) is PostgresException pg
        && (pg.SqlState == UndefinedTableSqlState || pg.SqlState == UnpopulatedMatviewSqlState);

    // ─── 1. Daily sales (Reports/PreciousMetal) ─────────────────────────────
    /// <summary>Daily sales.</summary>
    public async Task<DailySalesReportViewModel> DailySalesAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new DailySalesReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);

        List<DailySalesRow> rows;
        try
        {
            rows = await db.Database
                .SqlQuery<DailySalesRow>($@"
                    SELECT tenant_id,
                           kiosk_id,
                           sale_date,
                           kind,
                           transaction_count,
                           customer_count,
                           total_weight_g,
                           total_payout,
                           currency_code
                      FROM reporting.mv_daily_sales_summary
                     WHERE tenant_id  = {tenantId}
                       AND sale_date >= {fromDate}
                       AND sale_date <  {toDate}")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.DailySalesMatviewMissing(ex);
            return vm;
        }

        vm.Rows = rows.Select(r => new DailySalesReportRow
        {
            SaleDate = r.SaleDate,
            Kind = r.Kind,
            TransactionCount = r.TransactionCount,
            CustomerCount = r.CustomerCount,
            TotalWeightG = r.TotalWeightG,
            TotalPayout = r.TotalPayout,
            CurrencyCode = r.CurrencyCode,
        }).OrderBy(r => r.SaleDate).ThenBy(r => r.Kind).ToList();

        vm.TransactionCount = vm.Rows.Sum(r => r.TransactionCount);
        vm.CustomerCount = vm.Rows.Sum(r => r.CustomerCount);
        vm.TotalWeightG = vm.Rows.Sum(r => r.TotalWeightG);
        vm.TotalPayout = vm.Rows.Sum(r => r.TotalPayout);
        return vm;
    }

    // ─── 2. Holding vs sales (Reports/HoldingSales) ─────────────────────────
    /// <summary>Holding sales.</summary>
    public async Task<HoldingSalesReportViewModel> HoldingSalesAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new HoldingSalesReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);
        var (fromUtc, toUtc) = (DateRangeToUtc(fromDate), DateRangeToUtc(toDate));

        // Inventory (current holdings, per metal) — aggregated from the matview.
        List<DailyInventoryRow> inventory;
        try
        {
            inventory = await db.Database
                .SqlQuery<DailyInventoryRow>($@"
                    SELECT tenant_id,
                           kiosk_id,
                           snapshot_date,
                           metal,
                           total_weight_g,
                           avg_carat
                      FROM reporting.mv_daily_inventory_summary
                     WHERE tenant_id      = {tenantId}
                       AND snapshot_date >= {fromDate}
                       AND snapshot_date <  {toDate}")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.InventoryMatviewMissing(ex);
            inventory = new List<DailyInventoryRow>();
        }

        // Sales — aggregate transaction_items by metal in the same window.
        var fromOff = new DateTimeOffset(fromUtc);
        var toOff = new DateTimeOffset(toUtc);

        var soldByMetal = await (
            from t in db.Transactions.AsNoTracking()
            join i in db.TransactionItems.AsNoTracking() on t.Id equals i.TransactionId
            where t.TenantId == tenantId
                  && (t.Status == "paid" || t.Status == "accepted")
                  && t.OccurredAt >= fromOff && t.OccurredAt < toOff
            group new { t, i } by new { i.Metal, t.CurrencyCode } into g
            select new
            {
                g.Key.Metal,
                g.Key.CurrencyCode,
                SoldWeightG = g.Sum(x => x.i.BilledWeightG),
                SoldPayout = g.Sum(x => x.t.NetPayout),
            })
            .ToListAsync(ct);

        var holdingByMetal = inventory
            .GroupBy(r => r.Metal)
            .Select(g => new
            {
                Metal = g.Key,
                Weight = g.Sum(x => x.TotalWeightG),
                AsOf = g.Max(x => x.SnapshotDate),
            }).ToList();

        var metals = holdingByMetal.Select(h => h.Metal)
            .Union(soldByMetal.Select(s => s.Metal))
            .Distinct()
            .OrderBy(m => m);

        foreach (var metal in metals)
        {
            var h = holdingByMetal.FirstOrDefault(x => x.Metal == metal);
            var s = soldByMetal.FirstOrDefault(x => x.Metal == metal);
            vm.Rows.Add(new HoldingSalesReportRow
            {
                AsOfDate = h?.AsOf ?? fromDate.ToDateTime(TimeOnly.MinValue),
                Metal = metal,
                HoldingWeightG = h?.Weight ?? 0m,
                SoldWeightG = s?.SoldWeightG ?? 0m,
                SoldPayout = s?.SoldPayout ?? 0m,
                CurrencyCode = s?.CurrencyCode ?? string.Empty,
            });
        }

        vm.TotalHoldingWeightG = vm.Rows.Sum(r => r.HoldingWeightG);
        vm.TotalSoldWeightG = vm.Rows.Sum(r => r.SoldWeightG);
        vm.TotalSoldPayout = vm.Rows.Sum(r => r.SoldPayout);
        return vm;
    }

    // ─── 3. Carat × weight (Reports/CaratWeight) ────────────────────────────
    /// <summary>Carat weight.</summary>
    public async Task<CaratWeightReportViewModel> CaratWeightAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new CaratWeightReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);
        var fromOff = new DateTimeOffset(DateRangeToUtc(fromDate));
        var toOff = new DateTimeOffset(DateRangeToUtc(toDate));

        var grouped = await (
            from t in db.Transactions.AsNoTracking()
            join i in db.TransactionItems.AsNoTracking() on t.Id equals i.TransactionId
            where t.TenantId == tenantId
                  && t.OccurredAt >= fromOff && t.OccurredAt < toOff
            group i by new { i.BilledKarat, i.Metal } into g
            select new CaratWeightReportRow
            {
                BilledKarat = g.Key.BilledKarat,
                Metal = g.Key.Metal,
                TotalWeightG = g.Sum(x => x.BilledWeightG),
                ItemCount = g.Count(),
            })
            .ToListAsync(ct);

        vm.Rows = grouped.OrderBy(r => r.Metal).ThenByDescending(r => r.BilledKarat).ToList();
        vm.TotalWeightG = vm.Rows.Sum(r => r.TotalWeightG);
        vm.TotalItems = vm.Rows.Sum(r => r.ItemCount);
        return vm;
    }

    // ─── 4. Total expense (Reports/TotalExpense) ────────────────────────────
    /// <summary>Total expense.</summary>
    public async Task<TotalExpenseReportViewModel> TotalExpenseAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new TotalExpenseReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);

        // The expense_entries table is planned but not present in 0010–0030.
        // The view reporting.expenses_per_day exists as a placeholder backed by
        // reporting.expenses_source (a WHERE FALSE view) — querying it is safe
        // and yields zero rows on a fresh DB.
        try
        {
            var rows = await db.Database
                .SqlQuery<ExpenseDailyRow>($@"
                    SELECT expense_date,
                           amount,
                           currency_code
                      FROM reporting.expenses_per_day
                     WHERE tenant_id     = {tenantId}
                       AND expense_date >= {fromDate}
                       AND expense_date <  {toDate}")
                .ToListAsync(ct);

            vm.Rows = rows.Select(r => new TotalExpenseReportRow
            {
                ExpenseDate = r.ExpenseDate,
                Amount = r.Amount,
                CurrencyCode = r.CurrencyCode,
            }).OrderBy(r => r.ExpenseDate).ToList();

            vm.TotalAmount = vm.Rows.Sum(r => r.Amount);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.ExpensesViewMissing(ex);
            vm.ExpenseTableMissing = true;
        }
        catch (Exception ex)
        {
            // Defensive: if the view exists but reporting.expenses_source's
            // placeholder shape blew up, surface the empty/missing state
            // rather than 500ing the whole report page.
            logger.TotalExpenseQueryFailed(ex);
            vm.ExpenseTableMissing = true;
        }
        return vm;
    }

    // ─── 5. Worth (Reports/Worth) — latest inventory × latest metal rate ───
    /// <summary>Worth.</summary>
    public async Task<WorthReportViewModel> WorthAsync(DateTime? asOf, CancellationToken ct = default)
    {
        var vm = new WorthReportViewModel { AsOf = asOf ?? DateTime.UtcNow.Date };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        // Latest holdings per metal (most recent snapshot_date wins, summed across kiosks).
        List<WorthInventoryRow> inventory;
        try
        {
            inventory = await db.Database
                .SqlQuery<WorthInventoryRow>($@"
                    WITH latest AS (
                        SELECT tenant_id, kiosk_id, metal, MAX(snapshot_date) AS snapshot_date
                          FROM reporting.mv_daily_inventory_summary
                         WHERE tenant_id = {tenantId}
                         GROUP BY tenant_id, kiosk_id, metal
                    )
                    SELECT mv.metal,
                           AVG(mv.avg_carat),
                           SUM(mv.total_weight_g)
                      FROM reporting.mv_daily_inventory_summary mv
                      JOIN latest l USING (tenant_id, kiosk_id, metal, snapshot_date)
                     GROUP BY mv.metal")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.WorthInventoryMatviewMissing(ex);
            return vm;
        }

        // Latest rate per (metal, purity, currency).
        List<LatestMetalRateRow> rates;
        try
        {
            rates = await db.Database
                .SqlQuery<LatestMetalRateRow>($@"
                    SELECT DISTINCT ON (metal, purity_karat, currency_code)
                           metal,
                           purity_karat,
                           price_per_gram,
                           currency_code
                      FROM pricing.metal_rates
                     ORDER BY metal, purity_karat, currency_code, retrieved_at DESC")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.MetalRatesMissing(ex);
            rates = new List<LatestMetalRateRow>();
        }

        foreach (var inv in inventory)
        {
            // Pick the rate row whose karat is closest to the avg carat in stock.
            var rate = rates
                .Where(r => r.Metal == inv.Metal)
                .OrderBy(r => Math.Abs((double)(r.PurityKarat - (inv.AvgCarat ?? r.PurityKarat))))
                .FirstOrDefault();

            var ratePerGram = rate?.PricePerGram ?? 0m;
            var currency = rate?.CurrencyCode ?? string.Empty;
            vm.Rows.Add(new WorthReportRow
            {
                Metal = inv.Metal,
                AvgCarat = inv.AvgCarat,
                TotalWeightG = inv.TotalWeightG,
                LatestRatePerGram = ratePerGram,
                EstimatedWorth = inv.TotalWeightG * ratePerGram,
                CurrencyCode = currency,
            });
        }
        vm.TotalEstimatedWorth = vm.Rows.Sum(r => r.EstimatedWorth);
        return vm;
    }

    // ─── 6. Sales payout (Reports/SalesPayout) ──────────────────────────────
    /// <summary>Sales payout.</summary>
    public async Task<SalesPayoutReportViewModel> SalesPayoutAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new SalesPayoutReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);
        var fromOff = new DateTimeOffset(DateRangeToUtc(fromDate));
        var toOff = new DateTimeOffset(DateRangeToUtc(toDate));

        // payment.payouts joins to payment.payments via payment_id; payments
        // join transactions via transaction_id. We use raw SQL because the
        // payment.payments + payment_methods entities aren't in scope here.
        List<SalesPayoutRawRow> rows;
        try
        {
            rows = await db.Database
                .SqlQuery<SalesPayoutRawRow>($@"
                    SELECT t.occurred_at        AS occurred_on,
                           t.transaction_code   AS transaction_code,
                           t.total_amount       AS amount,
                           t.fees               AS fees,
                           t.net_payout         AS net_payout,
                           t.currency_code      AS currency_code,
                           t.status             AS transaction_status,
                           po.status            AS payout_status,
                           po.sent_at           AS payout_sent_at,
                           po.settled_at        AS payout_settled_at
                      FROM tx.transactions t
                      LEFT JOIN payment.payments  p  ON p.transaction_id = t.id
                      LEFT JOIN payment.payouts   po ON po.payment_id    = p.id
                     WHERE t.tenant_id   = {tenantId}
                       AND t.occurred_at >= {fromOff}
                       AND t.occurred_at <  {toOff}
                     ORDER BY t.occurred_at DESC")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.PaymentsTablesMissing(ex);
            return vm;
        }

        vm.Rows = rows.Select(r => new SalesPayoutReportRow
        {
            OccurredOn = r.OccurredOn.UtcDateTime,
            TransactionCode = r.TransactionCode,
            Amount = r.Amount,
            Fees = r.Fees,
            NetPayout = r.NetPayout,
            CurrencyCode = r.CurrencyCode,
            TransactionStatus = r.TransactionStatus,
            PayoutStatus = r.PayoutStatus,
            PayoutSentAt = r.PayoutSentAt?.UtcDateTime,
            PayoutSettledAt = r.PayoutSettledAt?.UtcDateTime,
        }).ToList();

        vm.TotalNetPayout = vm.Rows.Sum(r => r.NetPayout);
        vm.TotalFees = vm.Rows.Sum(r => r.Fees);
        return vm;
    }

    // ─── 7. Expected profit (Reports/ExpectedProfit) ────────────────────────
    /// <summary>Expected profit.</summary>
    public async Task<ExpectedProfitReportViewModel> ExpectedProfitAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new ExpectedProfitReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);

        List<DailyProfitRow> rows;
        try
        {
            rows = await db.Database
                .SqlQuery<DailyProfitRow>($@"
                    SELECT tenant_id,
                           kiosk_id,
                           profit_date,
                           gross_sales,
                           cost_of_acquisition,
                           expected_profit,
                           expenses,
                           currency_code
                      FROM reporting.mv_daily_profit_summary
                     WHERE tenant_id    = {tenantId}
                       AND profit_date >= {fromDate}
                       AND profit_date <  {toDate}")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.ExpectedProfitMatviewMissing(ex);
            return vm;
        }

        vm.Rows = rows.Select(r => new ExpectedProfitReportRow
        {
            ProfitDate = r.ProfitDate,
            GrossSales = r.GrossSales,
            CostOfAcquisition = r.CostOfAcquisition,
            ExpectedProfit = r.ExpectedProfit,
            CurrencyCode = r.CurrencyCode,
        }).OrderBy(r => r.ProfitDate).ToList();

        vm.TotalGrossSales = vm.Rows.Sum(r => r.GrossSales);
        vm.TotalCostOfAcquisition = vm.Rows.Sum(r => r.CostOfAcquisition);
        vm.TotalExpectedProfit = vm.Rows.Sum(r => r.ExpectedProfit);
        return vm;
    }

    // ─── 8. Profit after expense (Reports/ProfitAfterExpense) ───────────────
    /// <summary>Profit after expense.</summary>
    public async Task<ProfitAfterExpenseReportViewModel> ProfitAfterExpenseAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new ProfitAfterExpenseReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var (fromDate, toDate) = ExpandDateRange(from, to);

        List<DailyProfitRow> rows;
        try
        {
            rows = await db.Database
                .SqlQuery<DailyProfitRow>($@"
                    SELECT tenant_id,
                           kiosk_id,
                           profit_date,
                           gross_sales,
                           cost_of_acquisition,
                           expected_profit,
                           expenses,
                           currency_code
                      FROM reporting.mv_daily_profit_summary
                     WHERE tenant_id    = {tenantId}
                       AND profit_date >= {fromDate}
                       AND profit_date <  {toDate}")
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsEmptyReportingException(ex))
        {
            logger.ProfitAfterExpenseMatviewMissing(ex);
            return vm;
        }

        vm.Rows = rows.Select(r => new ProfitAfterExpenseReportRow
        {
            ProfitDate = r.ProfitDate,
            GrossSales = r.GrossSales,
            CostOfAcquisition = r.CostOfAcquisition,
            ExpectedProfit = r.ExpectedProfit,
            Expenses = r.Expenses,
            NetProfit = r.ExpectedProfit - r.Expenses,
            CurrencyCode = r.CurrencyCode,
        }).OrderBy(r => r.ProfitDate).ToList();

        vm.TotalGrossSales = vm.Rows.Sum(r => r.GrossSales);
        vm.TotalCostOfAcquisition = vm.Rows.Sum(r => r.CostOfAcquisition);
        vm.TotalExpectedProfit = vm.Rows.Sum(r => r.ExpectedProfit);
        vm.TotalExpenses = vm.Rows.Sum(r => r.Expenses);
        vm.TotalNetProfit = vm.Rows.Sum(r => r.NetProfit);
        return vm;
    }

    // ─── 9. Active promotional offers (Reports/Offer) ───────────────────────
    /// <summary>Offers.</summary>
    public async Task<OfferReportViewModel> OffersAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        var vm = new OfferReportViewModel { From = from, To = to };
        if (currentUser.TenantId is not Guid tenantId)
        {
            return vm;
        }

        var q = db.PromotionalOffers.AsNoTracking().Where(o => o.TenantId == tenantId);

        if (from is DateTime f)
        {
            var fo = new DateTimeOffset(DateTime.SpecifyKind(f.Date, DateTimeKind.Utc));
            q = q.Where(o => o.ValidityEnd >= fo);
        }
        if (to is DateTime t)
        {
            var toOff = new DateTimeOffset(DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Utc));
            q = q.Where(o => o.ValidityStart < toOff);
        }

        // Two-step projection: EF shaper can't coerce DateTimeOffset → DateTime in SQL.
        var rawOffers = await q
            .OrderByDescending(o => o.ValidityStart)
            .Select(o => new
            {
                o.Code,
                o.Description,
                o.DiscountPct,
                o.ValidityStart,
                o.ValidityEnd,
                o.IsActive,
            })
            .ToListAsync(ct);

        var rows = rawOffers.Select(o => new OfferReportItem
        {
            Code = o.Code,
            Description = o.Description,
            DiscountPct = o.DiscountPct,
            ValidityStart = o.ValidityStart.UtcDateTime,
            ValidityEnd = o.ValidityEnd.UtcDateTime,
            IsActive = o.IsActive,
        }).ToList();

        vm.Rows = rows;
        vm.ActiveCount = rows.Count(r => r.IsActive);
        vm.InactiveCount = rows.Count(r => !r.IsActive);
        return vm;
    }

    // ─── helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises the from/to inputs to a half-open <c>[from, to)</c> range
    /// expressed as <see cref="DateOnly"/>. <c>DateOnly</c> binds cleanly to
    /// PG <c>date</c> columns under Npgsql 9 — <c>DateTime</c> requires care
    /// around <c>Kind</c> for timestamps and has a habit of being inferred as
    /// timestamptz, which triggers implicit casts on a <c>date</c> column.
    /// </summary>
    private static (DateOnly fromDate, DateOnly toDate) ExpandDateRange(DateTime? from, DateTime? to)
    {
        var f = DateOnly.FromDateTime((from ?? DateTime.UtcNow.AddDays(-30)).Date);
        var t = DateOnly.FromDateTime((to ?? DateTime.UtcNow).Date).AddDays(1);
        return (f, t);
    }

    /// <summary>
    /// Lifts a <see cref="DateOnly"/> back to a UTC midnight <see cref="DateTime"/>
    /// for callers that need to bind a <c>timestamptz</c> parameter.
    /// </summary>
    private static DateTime DateRangeToUtc(DateOnly d)
        => DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    // Raw shape for the SalesPayout report — kept private because it leaks the
    // join columns; service code maps it onto the public ViewModel row.
    private sealed class SalesPayoutRawRow
    {
        public DateTimeOffset OccurredOn { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Fees { get; set; }
        public decimal NetPayout { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string? PayoutStatus { get; set; }
        public DateTimeOffset? PayoutSentAt { get; set; }
        public DateTimeOffset? PayoutSettledAt { get; set; }
    }
}
