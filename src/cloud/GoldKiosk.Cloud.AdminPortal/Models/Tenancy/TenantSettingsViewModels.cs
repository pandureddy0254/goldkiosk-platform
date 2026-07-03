using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Tenancy;

// ─── View-model tree ─────────────────────────────────────────────────────────
// Composed top-level VM rendered by TenantSettingsController.Index. Each tab
// has its own sub-DTO + form binding. Activation key history rows never
// surface cleartext — only the public-safe prefix + a short sha256 fragment.
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level VM for <c>/TenantSettings/Index</c>. Carries every tab payload so
/// the 6-tab page can render server-side and only one panel needs to be
/// visible at a time (toggled via the inline tab JS).
/// </summary>
public sealed class TenantSettingsViewModel
{
    /// <summary>Gets or sets the active tab.</summary>
    public string ActiveTab { get; init; } = "general";
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; init; }
    /// <summary>Gets or sets the tenant code.</summary>
    public string TenantCode { get; init; } = "";
    /// <summary>Gets or sets the tenant status.</summary>
    public string TenantStatus { get; init; } = "active";

    /// <summary>Gets or sets the general.</summary>
    public TenantGeneralForm General { get; init; } = new();
    /// <summary>Gets or sets the facts.</summary>
    public TenantFactsViewModel Facts { get; init; } = new();
    /// <summary>Gets or sets the branding.</summary>
    public TenantBrandingForm Branding { get; init; } = new();
    /// <summary>Gets or sets the features.</summary>
    public IReadOnlyList<FeatureFlagRow> Features { get; init; } = Array.Empty<FeatureFlagRow>();
    /// <summary>Gets or sets the retention.</summary>
    public TenantRetentionForm Retention { get; init; } = new();
    /// <summary>Gets or sets the compliance.</summary>
    public TenantComplianceViewModel Compliance { get; init; } = new();
    /// <summary>Gets or sets the activation.</summary>
    public TenantActivationViewModel Activation { get; init; } = new();
    /// <summary>Gets or sets the billing.</summary>
    public TenantBillingViewModel Billing { get; init; } = new();
}

// ─── General tab ────────────────────────────────────────────────────────────

/// <summary>Tenant general form.</summary>
public sealed class TenantGeneralForm
{
    /// <summary>Gets or sets the legal name.</summary>
    [Required, StringLength(200)]
    public string LegalName { get; set; } = "";

    /// <summary>Gets or sets the trading name.</summary>
    [StringLength(200)]
    public string? TradingName { get; set; }

    /// <summary>Gets or sets the trade licence no.</summary>
    [StringLength(80)]
    public string? TradeLicenceNo { get; set; }

    /// <summary>Gets or sets the vat registration.</summary>
    [StringLength(80)]
    public string? VatRegistration { get; set; }

    /// <summary>Gets or sets the primary admin.</summary>
    [StringLength(200)]
    public string PrimaryAdmin { get; set; } = "";

    /// <summary>standard | premium | bank — matches <c>tenancy.tenants.tier</c>.</summary>
    [Required]
    public string Tier { get; set; } = "standard";

    /// <summary>APAC | MIDEAST | EMEA | NORTHAM — matches <c>tenancy.regions.code</c>.</summary>
    [Required]
    public string DataResidencyRegion { get; set; } = "MIDEAST";

    /// <summary>Gets or sets the default currency code.</summary>
    [Required, StringLength(8)]
    public string DefaultCurrencyCode { get; set; } = "AED";

    /// <summary>Gets or sets the default currency label.</summary>
    public string? DefaultCurrencyLabel { get; set; }
}

/// <summary>Tenant facts view model.</summary>
public sealed class TenantFactsViewModel
{
    /// <summary>Gets or sets the tenant code.</summary>
    public string TenantCode { get; init; } = "";
    /// <summary>Gets or sets the onboarded at.</summary>
    public DateTimeOffset? OnboardedAt { get; init; }
    /// <summary>Gets or sets the days active.</summary>
    public int DaysActive { get; init; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; init; } = "active";
    /// <summary>Gets or sets the region label.</summary>
    public string RegionLabel { get; init; } = "";
    /// <summary>Gets or sets the azure region.</summary>
    public string AzureRegion { get; init; } = "";
    /// <summary>Gets or sets the kiosks deployed.</summary>
    public int KiosksDeployed { get; init; }
    /// <summary>Gets or sets the kiosks ordered.</summary>
    public int KiosksOrdered { get; init; }
    /// <summary>Gets or sets the active users.</summary>
    public int ActiveUsers { get; init; }
    /// <summary>Gets or sets the CRM lead code.</summary>
    public string? CrmLeadCode { get; init; }
}

// ─── Branding tab ───────────────────────────────────────────────────────────

/// <summary>Tenant branding form.</summary>
public sealed class TenantBrandingForm
{
    /// <summary>Gets or sets the logo URI.</summary>
    [StringLength(400)]
    public string? LogoUri { get; set; }
    /// <summary>Gets or sets the logo meta.</summary>
    public string? LogoMeta { get; set; }

    /// <summary>Hex (no leading #). Matches one of the named swatches in the UI.</summary>
    [StringLength(8)]
    public string AccentHex { get; set; } = "b8941f";

    /// <summary>Gets or sets the kiosk display name.</summary>
    [StringLength(120)]
    public string KioskDisplayName { get; set; } = "";

    /// <summary>Gets or sets the receipt footer.</summary>
    [StringLength(200)]
    public string ReceiptFooter { get; set; } = "";
}

// ─── Features tab ───────────────────────────────────────────────────────────

/// <summary>Feature flag row.</summary>
public sealed class FeatureFlagRow
{
    /// <summary>The stable code persisted to <c>tenancy.tenant_features.feature_code</c>.</summary>
    public string Code { get; init; } = "";
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; init; } = "";
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; init; } = "";
    /// <summary>Gets or sets a value indicating whether enabled.</summary>
    public bool Enabled { get; init; }
    /// <summary>Gets or sets the tag.</summary>
    public string? Tag { get; init; }   // "licensed", "beta" — purely cosmetic
    /// <summary>Gets or sets a value indicating whether tag is warn.</summary>
    public bool TagIsWarn { get; init; }
}

// ─── Activation keys tab ────────────────────────────────────────────────────

/// <summary>Tenant activation view model.</summary>
public sealed class TenantActivationViewModel
{
    /// <summary>Gets or sets the live keys.</summary>
    public int LiveKeys { get; init; }
    /// <summary>Gets or sets the issued all time.</summary>
    public int IssuedAllTime { get; init; }
    /// <summary>Gets or sets the consumed all time.</summary>
    public int ConsumedAllTime { get; init; }
    /// <summary>Gets or sets the revoked all time.</summary>
    public int RevokedAllTime { get; init; }
    /// <summary>Gets or sets the days since onboarding.</summary>
    public int DaysSinceOnboarding { get; init; }
    /// <summary>Gets or sets the last consumed at.</summary>
    public DateTimeOffset? LastConsumedAt { get; init; }

    /// <summary>Gets or sets the history.</summary>
    public IReadOnlyList<ActivationKeyHistoryRow> History { get; init; } =
        Array.Empty<ActivationKeyHistoryRow>();
}

/// <summary>
/// One row of the immutable activation-key history table. Cleartext keys are
/// NEVER returned by the service — only the public-safe <see cref="KeyPrefix"/>
/// (e.g. "AIKI-7F2A") plus the first 8 hex chars of the sha256.
/// </summary>
public sealed class ActivationKeyHistoryRow
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; init; }
    /// <summary>e.g. <c>AIKI-7F2A</c>.</summary>
    public string KeyPrefix { get; init; } = "";
    /// <summary>First 8 hex chars of the stored sha256 hash.</summary>
    public string HashShort { get; init; } = "";
    /// <summary>Gets or sets the issued to.</summary>
    public string IssuedTo { get; init; } = "";
    /// <summary>Gets or sets the issued to name.</summary>
    public string? IssuedToName { get; init; }
    /// <summary>Gets or sets the issued at.</summary>
    public DateTimeOffset IssuedAt { get; init; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset ExpiresAt { get; init; }
    /// <summary>One of: Consumed, Revoked, Live, Expired.</summary>
    public string State { get; init; } = "Live";
    /// <summary>Gets or sets the consumed at.</summary>
    public DateTimeOffset? ConsumedAt { get; init; }
    /// <summary>Gets or sets the consumed by name.</summary>
    public string? ConsumedByName { get; init; }
    /// <summary>Gets or sets the issued by actor.</summary>
    public string IssuedByActor { get; init; } = "crm";
    /// <summary>Gets or sets the revoked reason.</summary>
    public string? RevokedReason { get; init; }
}

// ─── Billing tab (mock) ─────────────────────────────────────────────────────

/// <summary>Tenant billing view model.</summary>
public sealed class TenantBillingViewModel
{
    /// <summary>Gets or sets the plan label.</summary>
    public string PlanLabel { get; init; } = "Bank";
    /// <summary>Gets or sets the plan tier.</summary>
    public string PlanTier { get; init; } = "enterprise tier";
    /// <summary>Gets or sets the mrr aed.</summary>
    public decimal MrrAed { get; init; }
    /// <summary>Gets or sets the per kiosk fee aed.</summary>
    public decimal PerKioskFeeAed { get; init; }
    /// <summary>Gets or sets the kiosk count.</summary>
    public int KioskCount { get; init; }
    /// <summary>Gets or sets the days to next invoice.</summary>
    public int DaysToNextInvoice { get; init; }
    /// <summary>Gets or sets the next invoice due.</summary>
    public DateTime? NextInvoiceDue { get; init; }
    /// <summary>Gets or sets the invoices.</summary>
    public IReadOnlyList<InvoiceRow> Invoices { get; init; } = Array.Empty<InvoiceRow>();
    /// <summary>Gets or sets the payment method title.</summary>
    public string PaymentMethodTitle { get; init; } = "Direct Debit · IBAN ••• 4823";
    /// <summary>Gets or sets the payment method sub.</summary>
    public string PaymentMethodSub { get; init; } = "Emirates NBD · Settlement Account";
    /// <summary>Gets or sets the billing contact email.</summary>
    public string BillingContactEmail { get; init; } = "finance@emiratesgold.ae";
    /// <summary>Gets or sets the billing contact name.</summary>
    public string BillingContactName { get; init; } = "CFO · Ahmed Khoury";
}

/// <summary>Invoice row.</summary>
public sealed class InvoiceRow
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; init; } = "";
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; init; } = "";
    /// <summary>Gets or sets the amount aed.</summary>
    public decimal AmountAed { get; init; }
    /// <summary>active | pending — drives the status-pill class.</summary>
    public string Status { get; init; } = "active";
    /// <summary>Gets or sets the status label.</summary>
    public string StatusLabel { get; init; } = "Paid";
}

// ─── Compliance tab ─────────────────────────────────────────────────────────

/// <summary>Tenant retention form.</summary>
public sealed class TenantRetentionForm
{
    /// <summary>365 / 1095 / 1825 / 2555 days. Persisted to <c>tenant_configs.pii_retention_days</c>.</summary>
    public int PiiRetentionDays { get; set; } = 2555;
    /// <summary>90 / 180 / 365 / 1095 days. Persisted to <c>tenant_configs.photo_retention_days</c>.</summary>
    public int PhotoRetentionDays { get; set; } = 365;
    /// <summary>1825 / 2555 / 3650 days. Stored in <c>config_json</c> on a synthetic feature row.</summary>
    public int TransactionRetentionDays { get; set; } = 2555;
    /// <summary>365 / 1095 / 2555 days. Stored in <c>config_json</c> on a synthetic feature row.</summary>
    public int AuditRetentionDays { get; set; } = 2555;
}

/// <summary>Tenant compliance view model.</summary>
public sealed class TenantComplianceViewModel
{
    /// <summary>Gets or sets the retention.</summary>
    public TenantRetentionForm Retention { get; init; } = new();
    /// <summary>Gets or sets the primary regulator.</summary>
    public string PrimaryRegulator { get; init; } = "DFSA · UAE";
    /// <summary>Gets or sets the compliance frame.</summary>
    public string ComplianceFrame { get; init; } = "DFSA / FSRA";
    /// <summary>Gets or sets the aml licence.</summary>
    public string? AmlLicence { get; init; }
    /// <summary>Gets or sets the lending licence.</summary>
    public string? LendingLicence { get; init; }
    /// <summary>Gets or sets the crypto licence.</summary>
    public string? CryptoLicence { get; init; }
    /// <summary>Gets or sets the last review on.</summary>
    public DateTime? LastReviewOn { get; init; }
    /// <summary>Gets or sets the next review due.</summary>
    public DateTime? NextReviewDue { get; init; }

    /// <summary>Gets or sets the dsr requests.</summary>
    public IReadOnlyList<DsrRequestRow> DsrRequests { get; init; } = Array.Empty<DsrRequestRow>();
}

/// <summary>Dsr request row.</summary>
public sealed class DsrRequestRow
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; init; } = "";
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; init; } = ""; // Right to access / erasure / portability
    /// <summary>Gets or sets the customer name.</summary>
    public string CustomerName { get; init; } = "";
    /// <summary>Gets or sets the age text.</summary>
    public string AgeText { get; init; } = ""; // "14 days ago"
    /// <summary>Gets or sets the sla text.</summary>
    public string SlaText { get; init; } = "";
    /// <summary>ok / pending / fail — feed-status class.</summary>
    public string Status { get; init; } = "ok";
}
