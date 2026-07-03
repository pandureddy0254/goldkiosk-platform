namespace GoldKiosk.Cloud.AdminPortal.Models.Layout;

// View-models for the shared layout chrome (header / sidebar / page-head / footer).
// Kept dumb on purpose — no logic, just bindings the partials read.

/// <summary>Layout chrome view model.</summary>
public sealed class LayoutChromeViewModel
{
    /// <summary>Gets or sets the user display name.</summary>
    public string UserDisplayName { get; init; } = "";
    /// <summary>Gets or sets the user initials.</summary>
    public string UserInitials { get; init; } = "";
    /// <summary>Gets or sets the user primary role.</summary>
    public string UserPrimaryRole { get; init; } = "Member";
}

/// <summary>Sidebar view model.</summary>
public sealed class SidebarViewModel
{
    /// <summary>
    /// Slug of the active nav link. One of: dashboard, kiosks, users, customers,
    /// merchants, sales, vouchers, reports, helpdesk, feedback, audit, monitoring,
    /// operations, user-management, roles, api-credentials, tenant-settings.
    /// Set per-view via <c>ViewBag.ActiveNav = "kiosks"</c>.
    /// </summary>
    public string ActiveNav { get; init; } = "";
    /// <summary>Gets or sets the tenant legal name.</summary>
    public string TenantLegalName { get; init; } = "";
    /// <summary>Gets or sets the tenant region.</summary>
    public string TenantRegion { get; init; } = "";
    /// <summary>Gets or sets the tenant code.</summary>
    public string TenantCode { get; init; } = "";
}

/// <summary>Page head view model.</summary>
public sealed class PageHeadViewModel
{
    /// <summary>Gets or sets the eyebrow.</summary>
    public string? Eyebrow { get; init; }
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; init; } = "";
    /// <summary>Gets or sets the subtitle.</summary>
    public string? Subtitle { get; init; }
    /// <summary>
    /// Raw HTML for the right-side action row (buttons). Pass via
    /// <c>ViewBag.PageHeadActions = "..."</c> from the view.
    /// </summary>
    public string? ActionsRaw { get; init; }
}

/// <summary>Footer view model.</summary>
public sealed class FooterViewModel
{
    /// <summary>Gets or sets the tenant code.</summary>
    public string TenantCode { get; init; } = "GK-DEV";
}

/// <summary>
/// Metadata footer rendered at the bottom of every Reports/* detail view.
/// Powers the "Report metadata" hairline grid (matview source, refresh cadence, freshness SLA).
/// </summary>
public sealed class ReportMetaCardModel
{
    /// <summary>Gets or sets the report code.</summary>
    public string ReportCode { get; init; } = "R-00";
    /// <summary>Gets or sets the matview.</summary>
    public string Matview { get; init; } = "mv_unknown";
    /// <summary>Gets or sets the cadence.</summary>
    public string Cadence { get; init; } = "On demand";
    /// <summary>Gets or sets the rows.</summary>
    public int Rows { get; init; }
    /// <summary>Gets or sets the schema hint.</summary>
    public string SchemaHint { get; init; } = "schema · reporting · azure-sql-ae-1";
    /// <summary>Gets or sets the sla text.</summary>
    public string SlaText { get; init; } = "P95 lag · 2.4 min · target ≤ 15 min";
}
