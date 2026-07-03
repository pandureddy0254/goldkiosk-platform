namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Resolves the permission codes granted to the signed-in user. Results are
/// cached per request in HttpContext.Items — repeated checks inside the same
/// request hit memory, not the database.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Returns true if the current user holds <paramref name="permissionCode"/>
    /// through any of their (non-revoked, non-expired) roles.
    /// </summary>
    Task<bool> HasAsync(string permissionCode, CancellationToken ct = default);

    /// <summary>
    /// Returns the full set of permission codes the current user holds.
    /// Empty (but never null) for anonymous callers.
    /// </summary>
    Task<IReadOnlySet<string>> GetGrantsAsync(CancellationToken ct = default);
}
