using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>View model for the partner directory (list + KPI strip + filter bar).</summary>
public class PartnerDirectoryViewModel
{
    /// <summary>One row per visible partner.</summary>
    public List<PartnerRow> Rows { get; set; } = [];

    /// <summary>KPI — partners with tenant_status = active.</summary>
    public int ActiveCount { get; set; }

    /// <summary>KPI — total visible partners.</summary>
    public int TotalCount { get; set; }

    /// <summary>KPI — sum of initial kiosk counts.</summary>
    public int KiosksDeployed { get; set; }

    /// <summary>KPI — live keys not yet consumed or revoked.</summary>
    public int KeysOutstanding { get; set; }

    /// <summary>
    /// Live MRR from subscriptions — grouped by currency (never summed across currencies).
    /// Key = ISO currency code (e.g. "USD"), Value = total MRR in that currency.
    /// </summary>
    public Dictionary<string, decimal> LiveMrrByCurrency { get; set; } = [];

    /// <summary>Search filter echoed in the querystring.</summary>
    public string? Search { get; set; }

    /// <summary>Status filter echoed in the querystring.</summary>
    public string? Status { get; set; }

    /// <summary>Region filter echoed in the querystring.</summary>
    public string? Region { get; set; }

    /// <summary>Key-state filter echoed in the querystring.</summary>
    public string? KeyState { get; set; }
}

/// <summary>One partner row in the directory table.</summary>
public class PartnerRow
{
    /// <summary>The partner entity.</summary>
    public Partner Partner { get; set; } = default!;

    /// <summary>The most relevant activation key (live preferred, else latest).</summary>
    public ActivationKey? LatestKey { get; set; }

    /// <summary>Live MRR sourced from subscriptions (trialing/active/past_due), not the partner snapshot.</summary>
    public decimal LiveMrr { get; set; }

    /// <summary>Currency code the <see cref="LiveMrr"/> figure is denominated in.</summary>
    public string LiveCurrency { get; set; } = "USD";
}
