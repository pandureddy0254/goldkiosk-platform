namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Per-render inputs for the proposal PDF generator.</summary>
public class ProposalRenderOptions
{
    /// <summary>Price per kiosk per year, in <see cref="Currency"/>.</summary>
    public decimal UnitPrice { get; set; } = 12500m;

    /// <summary>ISO currency code for the pricing table.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Number of kiosks quoted.</summary>
    public int KioskCount { get; set; } = 1;

    /// <summary>Scope section text; empty renders the default placeholder.</summary>
    public string ScopeSummary { get; set; } = "";

    /// <summary>Deliverables section text; empty renders the default bullet list.</summary>
    public string Deliverables { get; set; } = "";

    /// <summary>Payment-terms section text; empty renders the default terms.</summary>
    public string PaymentTerms { get; set; } = "";

    /// <summary>How many days the proposal remains valid.</summary>
    public int ValidityDays { get; set; } = 30;
}
