namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>crm.leads</c> — a sales opportunity moving through the pipeline stages.</summary>
public class Lead
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>Prospect company name.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Primary contact person's name.</summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>Primary contact email (citext column), when known.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Primary contact phone number, when known.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Prospect country, when known.</summary>
    public string? Country { get; set; }

    /// <summary>Prospect region or city, when known.</summary>
    public string? Region { get; set; }

    /// <summary>Where the lead came from — one of <see cref="LeadSource"/>.</summary>
    public string Source { get; set; } = LeadSource.Website;

    /// <summary>Current pipeline stage — one of <see cref="LeadStage"/>.</summary>
    public string Stage { get; set; } = LeadStage.New;

    /// <summary>Estimated deal value in <see cref="CurrencyCode"/>.</summary>
    public decimal? EstimatedValue { get; set; }

    /// <summary>ISO currency code for <see cref="EstimatedValue"/>.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Estimated number of kiosks the prospect would deploy.</summary>
    public int? EstimatedKioskCount { get; set; }

    /// <summary>Owning sales rep (crm.profiles id), or <c>null</c> when unassigned.</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>Free-form notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Reason recorded when the lead was lost.</summary>
    public string? LostReason { get; set; }

    /// <summary>Soft-delete timestamp; non-null rows are hidden from every list.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Proposal scope summary (single-proposal-per-lead, v1).</summary>
    public string? ScopeSummary { get; set; }

    /// <summary>Proposal deliverables text (single-proposal-per-lead, v1).</summary>
    public string? Deliverables { get; set; }

    /// <summary>Proposal payment terms text (single-proposal-per-lead, v1).</summary>
    public string? PaymentTerms { get; set; }

    /// <summary>How many days the proposal is valid. DB enforces 1–365; default 30.</summary>
    public int ValidityDays { get; set; } = 30;

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }
}
