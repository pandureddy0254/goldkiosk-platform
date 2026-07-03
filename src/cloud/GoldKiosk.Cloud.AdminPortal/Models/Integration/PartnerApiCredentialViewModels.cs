using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Integration;

/// <summary>
/// Single row in the API credentials catalogue. The cleartext app_key NEVER
/// appears here — only the public app_id, a short prefix of the secret, and
/// the lifecycle metadata.
/// </summary>
public sealed class PartnerApiCredentialRow
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets or sets the label.</summary>
    public string Label { get; init; } = "";
    /// <summary>Gets or sets the app id.</summary>
    public string AppId { get; init; } = "";

    /// <summary>First 12 chars of the cleartext key (e.g. "sk_live_a4f2"), captured at generation for the UI.</summary>
    public string AppKeyPrefix { get; init; } = "";

    /// <summary>Gets or sets the scopes.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>"live" or "test".</summary>
    public string Environment { get; init; } = "live";

    /// <summary>Gets or sets the last used at.</summary>
    public DateTimeOffset? LastUsedAt { get; init; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; init; }
    /// <summary>Gets or sets the revoked reason.</summary>
    public string? RevokedReason { get; init; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>"active" | "revoked".</summary>
    public string Status => RevokedAt is null ? "active" : "revoked";

    /// <summary>Display name of the user that created this credential.</summary>
    public string CreatedByName { get; init; } = "";
}

/// <summary>
/// Inbound form for the "Generate credential" drawer.
/// </summary>
public sealed class PartnerApiCredentialCreateRequest
{
    /// <summary>Gets or sets the label.</summary>
    [Required, StringLength(120, MinimumLength = 2)]
    public string Label { get; set; } = "";

    /// <summary>"live" or "test".</summary>
    [Required]
    public string Environment { get; set; } = "live";

    /// <summary>Permission codes — multi-select checkboxes in the drawer.</summary>
    public IList<string> Scopes { get; set; } = new List<string>();

    /// <summary>Optional CIDR allow-list, comma-separated. Stored as a jsonb array.</summary>
    public string? AllowedIpsCidr { get; set; }

    /// <summary>Optional expiry. Null = does not expire.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// Outcome of <see cref="Services.IPartnerApiCredentialService.CreateAsync"/>.
/// <see cref="ClearTextAppKey"/> and <see cref="ClearTextAppId"/> travel exactly once
/// from the service to the controller, into TempData, and out to the view for display.
/// </summary>
public sealed class PartnerApiCredentialCreateResult
{
    /// <summary>Gets or sets the row.</summary>
    public required PartnerApiCredentialRow Row { get; init; }

    /// <summary>The cleartext secret — surfaced ONCE. Never persisted.</summary>
    public required string ClearTextAppKey { get; init; }

    /// <summary>The cleartext app_id (also stored in the row, but echoed here for symmetry).</summary>
    public required string ClearTextAppId { get; init; }
}

/// <summary>
/// KPI strip values. Most are mocked in the prototype.
/// </summary>
public sealed class PartnerApiCredentialKpis
{
    /// <summary>Gets or sets the live count.</summary>
    public int LiveCount { get; init; }
    /// <summary>Gets or sets the revoked count.</summary>
    public int RevokedCount { get; init; }

    /// <summary>Total API calls in the last 30 days. Mocked at 0 in the prototype (no metrics pipeline yet).</summary>
    public long ApiCalls30d { get; init; }

    /// <summary>Average p95 latency in ms. Mocked.</summary>
    public int AvgP95Ms { get; init; }

    /// <summary>Rate-limit hits in the last 7 days. Mocked.</summary>
    public int RateLimitHits7d { get; init; }
}

/// <summary>
/// Top-level view-model bound to <c>Views/ApiCredentials/Index.cshtml</c>.
/// </summary>
public sealed class PartnerApiCredentialIndexViewModel
{
    /// <summary>Gets or sets the kpis.</summary>
    public PartnerApiCredentialKpis Kpis { get; init; } = new();
    /// <summary>Gets or sets the rows.</summary>
    public IReadOnlyList<PartnerApiCredentialRow> Rows { get; init; } = Array.Empty<PartnerApiCredentialRow>();

    /// <summary>"all" | "live" | "test".</summary>
    public string EnvironmentFilter { get; init; } = "all";

    /// <summary>True when the user has clicked the "Revoked" tab.</summary>
    public bool IncludeRevoked { get; init; }

    // ─── One-time-reveal envelope ───────────────────────────────────────────
    // Populated from TempData only when the user has just generated a credential.
    // Cleared on the next request. The cleartext NEVER hits a column.

    /// <summary>The cleartext app_key for a credential that was just created. Null otherwise.</summary>
    public string? ClearTextAppKey { get; init; }

    /// <summary>The cleartext app_id for a credential that was just created. Null otherwise.</summary>
    public string? ClearTextAppId { get; init; }

    /// <summary>The label of the credential that was just created. Null otherwise.</summary>
    public string? JustCreatedLabel { get; init; }

    /// <summary>The scopes of the credential that was just created.</summary>
    public IReadOnlyList<string> JustCreatedScopes { get; init; } = Array.Empty<string>();

    /// <summary>The environment of the credential that was just created ("live" | "test").</summary>
    public string? JustCreatedEnvironment { get; init; }

    /// <summary>Is null or empty.</summary>
    public bool HasJustCreatedKey => !string.IsNullOrEmpty(ClearTextAppKey);
}

/// <summary>
/// The canonical fixed list of scopes shown in the "Generate credential" drawer.
/// Kept in one place so the drawer + the validation share the same source of truth.
/// </summary>
public static class PartnerApiCredentialScopes
{
    /// <summary>Gets the all.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        "transactions:read",
        "transactions:write",
        "customers:read",
        "customers:write",
        "customers:unmask",
        "kiosks:read",
        "reports:read",
        "reports:export",
        "webhooks:write",
    };
}

/// <summary>
/// Envelope persisted in TempData between the POST→Create and the GET→Index redirect.
/// Serialised as JSON since TempData only carries primitives + strings.
/// </summary>
public sealed class JustCreatedCredentialFlash
{
    /// <summary>Gets or sets the app id.</summary>
    public string AppId { get; set; } = "";
    /// <summary>Gets or sets the app key.</summary>
    public string AppKey { get; set; } = "";
    /// <summary>Gets or sets the label.</summary>
    public string Label { get; set; } = "";
    /// <summary>Gets or sets the environment.</summary>
    public string Environment { get; set; } = "live";
    /// <summary>Gets or sets the scopes.</summary>
    public List<string> Scopes { get; set; } = new();
}
