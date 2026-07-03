using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// Deployment-level defaults for the kiosk config pull (karat window + feature switches).
/// Per-tenant/per-kiosk overrides move to the tenancy config tables when modeled
/// (TODO GK-TEN-1); until then these serve the whole deployment.
/// </summary>
public sealed class KioskConfigOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "KioskConfig";

    /// <summary>Gets the minimum accepted karat.</summary>
    [Range(1.0, 24.0)]
    public decimal MinKarat { get; init; } = 10.0m;

    /// <summary>Gets the maximum accepted karat.</summary>
    [Range(1.0, 24.0)]
    public decimal MaxKarat { get; init; } = 24.0m;

    /// <summary>Gets a value indicating whether pawn transactions are offered.</summary>
    public bool PawnEnabled { get; init; } = true;

    /// <summary>Gets a value indicating whether crypto-for-gold is enabled (workspace default: off).</summary>
    public bool CryptoEnabled { get; init; }

    /// <summary>
    /// Gets the payout methods available to kiosks. No in-code default: the configuration
    /// binder appends config values to pre-populated collections, so the value lives only
    /// in configuration and startup validation enforces its presence.
    /// </summary>
    [MinLength(1)]
    public IReadOnlyList<string> PayoutMethods { get; init; } = [];
}
