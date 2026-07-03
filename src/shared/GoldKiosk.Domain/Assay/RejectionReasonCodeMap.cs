namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Maps a Domain <see cref="RejectionReason"/> to the stable machine-readable rejection code
/// surfaced on the wire.
/// </summary>
/// <remarks>
/// The Domain layer must not depend on the Contracts assembly, so these string constants are
/// duplicated here and must stay in sync with
/// <c>GoldKiosk.Contracts.V1.Common.RejectionReasonCodes</c>:
/// <list type="bullet">
///   <item><description><c>item.gold_plated</c> — gold-plated item.</description></item>
///   <item><description><c>item.insufficient_purity</c> — karat/silver below the accepted floor.</description></item>
///   <item><description><c>item.unidentified</c> — excess disallowed alloy or a failed volume cross-check.</description></item>
/// </list>
/// </remarks>
public static class RejectionReasonCodeMap
{
    /// <summary>The item is gold plated (mirrors <c>RejectionReasonCodes.ItemGoldPlated</c>).</summary>
    public const string GoldPlated = "item.gold_plated";

    /// <summary>The measured purity is below the accepted minimum (mirrors <c>RejectionReasonCodes.ItemInsufficientPurity</c>).</summary>
    public const string InsufficientPurity = "item.insufficient_purity";

    /// <summary>The item could not be identified as acceptable metal (mirrors <c>RejectionReasonCodes.ItemUnidentified</c>).</summary>
    public const string Unidentified = "item.unidentified";

    /// <summary>Maps a rejection reason to its stable wire code.</summary>
    /// <param name="reason">The Domain rejection reason.</param>
    /// <returns>The stable machine-readable code.</returns>
    public static string ToCode(RejectionReason reason) => reason switch
    {
        RejectionReason.GoldPlated => GoldPlated,
        RejectionReason.LessThanAcceptableGoldKarat => InsufficientPurity,
        RejectionReason.LessThanAcceptableSilverPercent => InsufficientPurity,
        _ => Unidentified,
    };
}
