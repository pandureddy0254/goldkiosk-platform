namespace GoldKiosk.Contracts.V1.Cloud.Content;

/// <summary>
/// A UI language served to a kiosk from <c>GET /api/v1/languages</c>.
/// </summary>
/// <param name="Id">The language id.</param>
/// <param name="Code">The BCP-47 code, e.g. <c>en</c>, <c>hi</c>.</param>
/// <param name="NativeName">The language name in its own script.</param>
/// <param name="EnglishName">The language name in English.</param>
/// <param name="DisplayOrder">The kiosk display order.</param>
public sealed record LanguageDto(
    Guid Id,
    string Code,
    string NativeName,
    string EnglishName,
    int DisplayOrder);
