namespace GoldKiosk.Contracts.V1.Common;

/// <summary>
/// Stable machine-readable rejection reason codes surfaced to the kiosk UI
/// (localized display text is resolved edge-side from the reason catalogue).
/// </summary>
public static class RejectionReasonCodes
{
    /// <summary>The tray closed with nothing detectable on it.</summary>
    public const string ItemEmptyTray = "item.empty_tray";

    /// <summary>More than one item was placed; one item at a time.</summary>
    public const string ItemMultipleItems = "item.multiple_items";

    /// <summary>The item could not be identified.</summary>
    public const string ItemUnidentified = "item.unidentified";

    /// <summary>The item type is not accepted (e.g. coins, watches).</summary>
    public const string ItemUnacceptedType = "item.unaccepted_type";

    /// <summary>The item weighs less than the accepted minimum.</summary>
    public const string ItemUnderweight = "item.underweight";

    /// <summary>The item is gold plated, not solid precious metal.</summary>
    public const string ItemGoldPlated = "item.gold_plated";

    /// <summary>The measured purity is below the accepted minimum.</summary>
    public const string ItemInsufficientPurity = "item.insufficient_purity";

    /// <summary>The customer is under the legal minimum age.</summary>
    public const string KycUnderage = "kyc.underage";

    /// <summary>The presented identity document is expired.</summary>
    public const string KycIdExpired = "kyc.id_expired";

    /// <summary>The presented document is not a government-issued ID.</summary>
    public const string KycNotGovtId = "kyc.not_govt_id";

    /// <summary>The customer is on a blocklist.</summary>
    public const string KycBlacklisted = "kyc.blacklisted";

    /// <summary>The selfie did not match the ID photo.</summary>
    public const string KycFaceMismatch = "kyc.face_mismatch";

    /// <summary>The cassettes cannot cover the payout amount in cash.</summary>
    public const string PayoutInsufficientCash = "payout.insufficient_cash";
}
