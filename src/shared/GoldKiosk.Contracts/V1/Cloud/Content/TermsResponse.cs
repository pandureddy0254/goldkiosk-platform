namespace GoldKiosk.Contracts.V1.Cloud.Content;

/// <summary>
/// Response body for <c>GET /api/v1/terms</c> — the tenant's active terms &amp; conditions
/// (global default when the tenant has none).
/// </summary>
/// <param name="Version">The terms version identifier, e.g. <c>2026-06-01.v3</c>.</param>
/// <param name="Title">The document title.</param>
/// <param name="EffectiveDate">The date the document took effect.</param>
/// <param name="Sections">The document sections in display order.</param>
public sealed record TermsResponse(
    string Version,
    string Title,
    DateOnly EffectiveDate,
    IReadOnlyList<TermsSectionDto> Sections);
