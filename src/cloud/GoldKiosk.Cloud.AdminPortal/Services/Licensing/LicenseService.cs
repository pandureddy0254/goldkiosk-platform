using GoldKiosk.Cloud.AdminPortal.Logging;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>License service.</summary>
public sealed class LicenseService : ILicenseService
{
    private readonly JwksClient _jwks;
    private readonly RevocationClient _revocation;
    private readonly ILicenseStore _store;
    private readonly ILogger<LicenseService> _logger;

    /// <summary>Initializes a new instance of the <see cref="LicenseService"/> class.</summary>
    public LicenseService(
        JwksClient jwks,
        RevocationClient revocation,
        ILicenseStore store,
        ILogger<LicenseService> logger)
    {
        _jwks = jwks;
        _revocation = revocation;
        _store = store;
        _logger = logger;
    }

    /// <summary>Get active.</summary>
    public Task<StoredLicense?> GetActiveAsync(CancellationToken ct = default)
        => _store.GetActiveAsync(ct);

    /// <summary>Is active valid.</summary>
    public async Task<bool> IsActiveValidAsync(CancellationToken ct = default)
    {
        var active = await _store.GetActiveAsync(ct);
        if (active is null)
        {
            return false;
        }

        var outcome = ReverifyStored(active);
        return outcome.Valid;
    }

    /// <summary>Activate.</summary>
    public async Task<VerifyOutcome> ActivateAsync(string token, CancellationToken ct = default)
    {
        if (_revocation.IsRevoked(token))
        {
            return new(false, null, "This license has been revoked. Contact your partner-success representative.");
        }

        var outcome = _jwks.Verifier.Verify(token);
        if (!outcome.Valid)
        {
            return outcome;
        }

        var entry = new StoredLicense
        {
            RawToken = token.Trim(),
            License = outcome.License!,
            ActivatedAt = DateTimeOffset.UtcNow,
        };
        await _store.SaveAsync(entry, setActive: true, ct);
        _logger.LicenseActivated(entry.License.TenantId, entry.License.LegalName, entry.License.PlanCode, entry.License.KioskCap);
        return outcome;
    }

    /// <summary>Reverify stored.</summary>
    public VerifyOutcome ReverifyStored(StoredLicense stored)
    {
        if (_revocation.IsRevoked(stored.RawToken))
        {
            return new(false, stored.License, "This license has been revoked.");
        }

        return _jwks.Verifier.Verify(stored.RawToken);
    }
}
