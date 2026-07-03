using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Gets the i kiosk security token service.</summary>
/// <summary>I kiosk security token service.</summary>
public interface IKioskSecurityTokenService
{
    /// <summary>List.</summary>
    Task<KioskSecurityTokenList> ListAsync(Guid? kioskId, int pageSize, int pageNo, CancellationToken ct = default);

    /// <summary>
    /// Issues a new token for the kiosk. Returns the raw token ONCE in
    /// <see cref="KioskSecurityTokenList.NewlyIssuedRawToken"/>; only the
    /// SHA-256 hash is persisted.
    /// </summary>
    Task<KioskSecurityTokenList> IssueAsync(Guid kioskId, TimeSpan? validity, CancellationToken ct = default);

    /// <summary>Revoke.</summary>
    Task<OperationResult> RevokeAsync(Guid tokenId, CancellationToken ct = default);
}
