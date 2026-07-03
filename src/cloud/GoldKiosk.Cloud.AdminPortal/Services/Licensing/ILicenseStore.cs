namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Local-disk persistence for verified licenses. One JSON file per tenant under
/// App_Data/licenses/, plus a small "active.txt" pointer that says which tenant
/// is currently bound to this install (lets us answer "is anything activated?"
/// without scanning the directory).
/// </summary>
public interface ILicenseStore
{
    /// <summary>Get active.</summary>
    Task<StoredLicense?> GetActiveAsync(CancellationToken ct = default);
    /// <summary>Get by tenant.</summary>
    Task<StoredLicense?> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    /// <summary>Save.</summary>
    Task SaveAsync(StoredLicense entry, bool setActive, CancellationToken ct = default);
    /// <summary>Clear active.</summary>
    Task ClearActiveAsync(CancellationToken ct = default);
}

/// <summary>
/// What we persist: the license payload + the raw token (we need the raw token
/// for the revocation-prefix check and for re-verification after key rotation).
/// </summary>
public sealed class StoredLicense
{
    /// <summary>Gets or sets the raw token.</summary>
    public string RawToken { get; set; } = "";
    /// <summary>Gets or sets the license.</summary>
    public License License { get; set; } = new();
    /// <summary>Gets or sets the activated at.</summary>
    public DateTimeOffset ActivatedAt { get; set; }
}
