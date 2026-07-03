namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Mirrors <c>crm.crm_role</c> (text column). superadmin &gt;= sales_manager for access checks.</summary>
public static class CrmRole
{
    /// <summary>Sales manager — sees every lead/partner and may issue or revoke license keys.</summary>
    public const string SalesManager = "sales_manager";

    /// <summary>Sales representative — sees only their own leads and descendants.</summary>
    public const string SalesRep = "sales_rep";

    /// <summary>Superadmin — full access including member management.</summary>
    public const string Superadmin = "superadmin";
}

/// <summary>Mirrors <c>public.lead_source</c>.</summary>
public static class LeadSource
{
    /// <summary>Lead arrived via the public website.</summary>
    public const string Website = "website";

    /// <summary>Lead arrived via an inbound phone call.</summary>
    public const string InboundCall = "inbound_call";

    /// <summary>Lead referred by an existing partner.</summary>
    public const string PartnerReferral = "partner_referral";

    /// <summary>Lead met at a conference or trade show.</summary>
    public const string Conference = "conference";

    /// <summary>Lead sourced by outbound prospecting.</summary>
    public const string Outbound = "outbound";
}

/// <summary>Mirrors <c>public.lead_stage</c>.</summary>
public static class LeadStage
{
    /// <summary>Freshly created, not yet worked.</summary>
    public const string New = "new";

    /// <summary>First contact has been made.</summary>
    public const string Contacted = "contacted";

    /// <summary>Qualified as a real opportunity.</summary>
    public const string Qualified = "qualified";

    /// <summary>A proposal has been prepared/sent.</summary>
    public const string Proposal = "proposal";

    /// <summary>Won — converted to a partner.</summary>
    public const string Won = "won";

    /// <summary>Lost — closed without conversion.</summary>
    public const string Lost = "lost";
}

/// <summary>Mirrors <c>public.tenant_status</c>.</summary>
public static class TenantStatus
{
    /// <summary>Tenant is being provisioned.</summary>
    public const string Provisioning = "provisioning";

    /// <summary>Tenant is live.</summary>
    public const string Active = "active";

    /// <summary>Tenant access is suspended.</summary>
    public const string Suspended = "suspended";

    /// <summary>Tenant has churned.</summary>
    public const string Churned = "churned";
}

/// <summary>Mirrors <c>public.subscription_status</c>.</summary>
public static class SubscriptionStatus
{
    /// <summary>In the trial window.</summary>
    public const string Trialing = "trialing";

    /// <summary>Active and paid up.</summary>
    public const string Active = "active";

    /// <summary>Payment overdue.</summary>
    public const string PastDue = "past_due";

    /// <summary>Suspended by operations.</summary>
    public const string Suspended = "suspended";

    /// <summary>Cancelled by the partner.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>Term ended without renewal.</summary>
    public const string Expired = "expired";
}

/// <summary>Mirrors <c>public.billing_cycle</c>.</summary>
public static class BillingCycle
{
    /// <summary>Billed every month.</summary>
    public const string Monthly = "monthly";

    /// <summary>Billed every quarter.</summary>
    public const string Quarterly = "quarterly";

    /// <summary>Billed every year.</summary>
    public const string Annual = "annual";

    /// <summary>Billed every two years.</summary>
    public const string Biennial = "biennial";
}

/// <summary>Supported currencies — matches the CHECK constraint on every monetary table.</summary>
public static class SupportedCurrencies
{
    /// <summary>Every ISO currency code the schema accepts on monetary columns.</summary>
    public static readonly IReadOnlyList<string> All =
        ["USD", "AED", "INR", "EUR", "GBP", "SAR", "QAR", "KWD", "BHD", "OMR"];

    /// <summary>Returns <c>true</c> when <paramref name="code"/> (case-insensitive) is an accepted currency.</summary>
    /// <param name="code">ISO 4217 currency code to test.</param>
    public static bool IsSupported(string code) =>
        All.Contains(code?.ToUpperInvariant() ?? string.Empty);
}
