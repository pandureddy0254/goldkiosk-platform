namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>I license service.</summary>
public interface ILicenseService
{
    /// <summary>Currently bound (active) license, or null on a fresh install.</summary>
    Task<StoredLicense?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>True if there's an active license, it's signed by a known kid,
    /// not expired, and not revoked.</summary>
    Task<bool> IsActiveValidAsync(CancellationToken ct = default);

    /// <summary>Verify a pasted token and, if valid, persist + bind as active.</summary>
    Task<VerifyOutcome> ActivateAsync(string token, CancellationToken ct = default);

    /// <summary>Re-verify a stored token against the current keys + revocation list.
    /// Cheap — call this from the gate middleware to detect rotation + revocation.</summary>
    VerifyOutcome ReverifyStored(StoredLicense stored);
}
