namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// The measured item facts the edge sends to the cloud for a binding offer. Minimal by
/// design: the gateway derives the wire request's karat and category from these. Karat and
/// PurityPercent are two views of the same assay — the gateway derives karat from
/// <paramref name="GoldPercent"/>.
/// </summary>
/// <param name="WeightGrams">The measured weight in grams.</param>
/// <param name="GoldPercent">The assayed gold content in percent (0–100).</param>
/// <param name="MetalType">The assayed metal, e.g. <c>gold</c>, <c>silver</c>.</param>
/// <param name="Kind">The transaction kind: <c>sale</c> or <c>pawn</c>.</param>
public sealed record CloudOfferInput(
    decimal WeightGrams,
    decimal GoldPercent,
    string MetalType,
    string Kind);
