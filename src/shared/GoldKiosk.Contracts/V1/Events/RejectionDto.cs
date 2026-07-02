using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// Why an item or customer was rejected, with UI recovery routing.
/// </summary>
/// <param name="ReasonCode">The stable reason code (see <see cref="RejectionReasonCodes"/>), e.g. <c>item.multiple_items</c>.</param>
/// <param name="Display">The customer-facing display text (localized edge-side).</param>
/// <param name="Recovery">The recovery routing hint for the UI, e.g. <c>retry_place_item</c>.</param>
public sealed record RejectionDto(string ReasonCode, string Display, string Recovery);
