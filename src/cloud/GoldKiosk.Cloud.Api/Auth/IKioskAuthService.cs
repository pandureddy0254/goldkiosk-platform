using GoldKiosk.Contracts.V1.Cloud.Auth;

namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// Authenticates kiosk machines (Code + PIN → JWT). Adapted from platform2's
/// <c>KioskAuthService</c>; evolves toward per-device certificate identity per ADR 0003.
/// </summary>
public interface IKioskAuthService
{
    /// <summary>Attempts a kiosk login.</summary>
    /// <param name="code">The kiosk fleet code.</param>
    /// <param name="pin">The kiosk PIN.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The token response, or <see langword="null"/> when the credentials do not authenticate.</returns>
    Task<KioskLoginResponse?> LoginAsync(string code, string pin, CancellationToken cancellationToken = default);
}
