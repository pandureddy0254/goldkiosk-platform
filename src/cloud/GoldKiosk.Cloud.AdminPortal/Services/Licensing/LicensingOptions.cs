namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Bound from the "Licensing" section of appsettings.json. Air-gapped customers
/// pin <see cref="PinnedJwks"/> and <see cref="PinnedRevocationList"/> and the
/// app never reaches out to <see cref="CrmBaseUrl"/>.
/// </summary>
public sealed class LicensingOptions
{
    /// <summary>Section name.</summary>
    public const string SectionName = "Licensing";

    /// <summary>Gets or sets the CRM base URL.</summary>
    public string CrmBaseUrl { get; set; } = "https://crm.goldkiosk.ai";

    /// <summary>
    /// Pinned JWKS JSON document. If set, the app uses this in addition to (or
    /// instead of) the live fetch from CrmBaseUrl. The fetched JWKS overrides
    /// pinned entries with the same kid, so pinning is a safe fallback rather
    /// than a freeze.
    /// </summary>
    public string? PinnedJwks { get; set; }

    /// <summary>
    /// Pinned revocation-list JSON. Optional — defaults to "no revocations" if
    /// the live fetch fails and nothing is pinned.
    /// </summary>
    public string? PinnedRevocationList { get; set; }

    /// <summary>
    /// Set true for air-gapped installs that should never call out to the CRM.
    /// Requires PinnedJwks to be populated or no token will verify.
    /// </summary>
    public bool AirGapped { get; set; }

    /// <summary>Gets or sets the JWKS refresh minutes.</summary>
    public int JwksRefreshMinutes { get; set; } = 360;  // 6h
    /// <summary>Gets or sets the revocation refresh minutes.</summary>
    public int RevocationRefreshMinutes { get; set; } = 60;   // 1h

    /// <summary>
    /// If true, the license-gate middleware redirects unlicensed requests to
    /// /License/Activate. Set false for tests / first-run dev seeding.
    /// </summary>
    public bool EnforceLicense { get; set; } = true;
}
