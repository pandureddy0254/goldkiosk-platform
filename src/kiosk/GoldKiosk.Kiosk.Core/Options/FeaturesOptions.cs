using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Core.Options;

/// <summary>
/// Options for the <c>Features</c> configuration section — feature flags surfaced to the
/// UI at session begin and enforced by the session flow. Cloud static config can override
/// per tenant/kiosk (configuration-and-operations §1).
/// </summary>
public sealed class FeaturesOptions
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "Features";

    /// <summary>Whether the pawn service is offered alongside sell.</summary>
    public bool PawnEnabled { get; set; } = true;

    /// <summary>Whether crypto payout is offered (tenant opt-in; off by default).</summary>
    public bool CryptoEnabled { get; set; }

    /// <summary>Whether the fingerprint identity step is required.</summary>
    public bool FingerprintRequired { get; set; }

    /// <summary>
    /// The payout methods available on this kiosk, e.g. <c>cash</c>, <c>bank_transfer</c>,
    /// <c>debit_card</c>. Deliberately defaulted empty — the config binder appends to
    /// pre-populated lists, and a kiosk with no configured payout method must fail fast.
    /// </summary>
    [MinLength(1, ErrorMessage = "Features:PayoutMethods must contain at least one method.")]
    public IList<string> PayoutMethods { get; init; } = [];
}
