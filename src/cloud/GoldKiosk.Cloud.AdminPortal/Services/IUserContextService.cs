namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Per-request lookups for the signed-in user's display name, primary role
/// and tenant name. Results are cached in <see cref="HttpContext.Items"/> so
/// the shared layout can call these on every page render without re-querying
/// the database.
/// </summary>
public interface IUserContextService
{
    /// <summary>Get display name.</summary>
    Task<string> GetDisplayNameAsync(CancellationToken ct = default);

    /// <summary>
    /// Two-letter initials from the user's first + last name ("SM" for Sara Al Mansoori).
    /// Falls back to the first two letters of the email if no name claims are present.
    /// </summary>
    Task<string> GetInitialsAsync(CancellationToken ct = default);

    /// <summary>Get primary role name.</summary>
    Task<string?> GetPrimaryRoleNameAsync(CancellationToken ct = default);

    /// <summary>Get tenant legal name.</summary>
    Task<string?> GetTenantLegalNameAsync(CancellationToken ct = default);

    /// <summary>Tenant code (e.g. "GK-DEV", "EGB-AE-001"), shown in the sidebar foot.</summary>
    Task<string?> GetTenantCodeAsync(CancellationToken ct = default);

    /// <summary>Free-form region label ("UAE · primary · production"), shown in the sidebar foot.</summary>
    Task<string?> GetTenantRegionAsync(CancellationToken ct = default);
}
