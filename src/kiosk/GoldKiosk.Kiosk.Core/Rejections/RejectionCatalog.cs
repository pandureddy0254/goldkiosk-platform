using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Events;

namespace GoldKiosk.Kiosk.Core.Rejections;

/// <summary>
/// The edge-side rejection reason catalogue: maps stable reason codes
/// (<see cref="RejectionReasonCodes"/>) to customer-facing display text and UI recovery
/// routing. Localization replaces the display strings later; codes never change.
/// </summary>
public static class RejectionCatalog
{
    private static readonly Dictionary<string, (string Display, string Recovery)> _catalog =
        new(StringComparer.Ordinal)
        {
            [RejectionReasonCodes.ItemEmptyTray] = ("We couldn't find an item on the tray", "retry_place_item"),
            [RejectionReasonCodes.ItemMultipleItems] = ("Please place one item at a time", "retry_place_item"),
            [RejectionReasonCodes.ItemUnidentified] = ("We couldn't identify this item", "retry_place_item"),
            [RejectionReasonCodes.ItemUnacceptedType] = ("This item type isn't accepted", "return_item"),
            [RejectionReasonCodes.ItemUnderweight] = ("This item is below the minimum weight", "return_item"),
            [RejectionReasonCodes.ItemGoldPlated] = ("This item appears to be plated", "return_item"),
            [RejectionReasonCodes.ItemInsufficientPurity] = ("The precious-metal content is below our minimum", "return_item"),
            [RejectionReasonCodes.KycUnderage] = ("You must be 18 or older to trade", "abort"),
            [RejectionReasonCodes.KycIdExpired] = ("This ID has expired", "retry_id_scan"),
            [RejectionReasonCodes.KycNotGovtId] = ("Please use a government-issued ID", "retry_id_scan"),
            [RejectionReasonCodes.KycBlacklisted] = ("We can't proceed with this transaction", "abort"),
            [RejectionReasonCodes.KycFaceMismatch] = ("We couldn't match your face to the ID", "retry_face_match"),
            [RejectionReasonCodes.PayoutInsufficientCash] = ("Cash unavailable for this amount", "choose_other_method"),
        };

    /// <summary>Builds the wire rejection payload for a stable reason code.</summary>
    /// <param name="reasonCode">The reason code, e.g. <c>item.underweight</c>.</param>
    /// <returns>The rejection with display text and recovery routing.</returns>
    public static RejectionDto FromCode(string reasonCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        return _catalog.TryGetValue(reasonCode, out (string Display, string Recovery) entry)
            ? new RejectionDto(reasonCode, entry.Display, entry.Recovery)
            : new RejectionDto(reasonCode, "We can't proceed with this item", "return_item");
    }
}
