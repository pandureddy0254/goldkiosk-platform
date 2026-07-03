namespace GoldKiosk.Cloud.AdminPortal.Services.Reports.Rows;

// Lightweight row classes for raw-SQL projections via db.Database.SqlQuery<T>.
// EF Core matches columns to public settable properties (case-insensitive,
// snake_case-aware via the naming convention). Keep these in sync with the
// SELECT lists in the corresponding service methods.

/// <summary>Daily sales row.</summary>
public sealed class DailySalesRow
{
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the sale date.</summary>
    public DateTime SaleDate { get; set; }
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction count.</summary>
    public int TransactionCount { get; set; }
    /// <summary>Gets or sets the customer count.</summary>
    public int CustomerCount { get; set; }
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the total payout.</summary>
    public decimal TotalPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Daily inventory row.</summary>
public sealed class DailyInventoryRow
{
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the snapshot date.</summary>
    public DateTime SnapshotDate { get; set; }
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the avg carat.</summary>
    public decimal? AvgCarat { get; set; }
}

/// <summary>Daily profit row.</summary>
public sealed class DailyProfitRow
{
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the profit date.</summary>
    public DateTime ProfitDate { get; set; }
    /// <summary>Gets or sets the gross sales.</summary>
    public decimal GrossSales { get; set; }
    /// <summary>Gets or sets the cost of acquisition.</summary>
    public decimal CostOfAcquisition { get; set; }
    /// <summary>Gets or sets the expected profit.</summary>
    public decimal ExpectedProfit { get; set; }
    /// <summary>Gets or sets the expenses.</summary>
    public decimal Expenses { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Expense daily row.</summary>
public sealed class ExpenseDailyRow
{
    /// <summary>Gets or sets the expense date.</summary>
    public DateTime ExpenseDate { get; set; }
    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Worth inventory row.</summary>
public sealed class WorthInventoryRow
{
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the avg carat.</summary>
    public decimal? AvgCarat { get; set; }
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
}

/// <summary>Latest metal rate row.</summary>
public sealed class LatestMetalRateRow
{
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the purity karat.</summary>
    public decimal PurityKarat { get; set; }
    /// <summary>Gets or sets the price per gram.</summary>
    public decimal PricePerGram { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}
