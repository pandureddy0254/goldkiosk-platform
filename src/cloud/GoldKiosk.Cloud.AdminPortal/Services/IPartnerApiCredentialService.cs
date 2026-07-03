using GoldKiosk.Cloud.AdminPortal.Models.Integration;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Service for the partner-facing API credentials catalogue.
/// Surfaces live + revoked credentials, lets primary admins generate new pairs
/// (returning the cleartext app_key exactly once), and supports revocation.
/// </summary>
public interface IPartnerApiCredentialService
{
    /// <summary>List credentials for the current tenant. Filter by environment (live/test) and revoked state.</summary>
    Task<IReadOnlyList<PartnerApiCredentialRow>> ListAsync(string? environment, bool includeRevoked, CancellationToken ct = default);

    /// <summary>
    /// Create a new credential pair. The cleartext app_key on the result is returned
    /// to the caller exactly once — the DB stores only the SHA-256 hash.
    /// </summary>
    Task<PartnerApiCredentialCreateResult> CreateAsync(PartnerApiCredentialCreateRequest req, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Revoke a credential. Idempotent: revoking an already-revoked credential is a no-op.</summary>
    Task RevokeAsync(Guid credentialId, string reason, Guid actorUserId, CancellationToken ct = default);

    /// <summary>KPI strip values surfaced on the index view.</summary>
    Task<PartnerApiCredentialKpis> GetKpisAsync(CancellationToken ct = default);
}
