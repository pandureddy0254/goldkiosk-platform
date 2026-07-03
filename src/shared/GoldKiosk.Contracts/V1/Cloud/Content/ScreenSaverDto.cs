namespace GoldKiosk.Contracts.V1.Cloud.Content;

/// <summary>
/// One attract-loop slide served from <c>GET /api/v1/screensavers</c>.
/// </summary>
/// <param name="Code">The slide code.</param>
/// <param name="ImageUrl">The media URL (Blob-hosted; kiosks cache by ETag).</param>
/// <param name="MediaType">The media type: <c>image</c> or <c>video</c>.</param>
/// <param name="DurationSeconds">The display duration; <see langword="null"/> = media default.</param>
/// <param name="DisplayOrder">The rotation order.</param>
public sealed record ScreenSaverDto(
    string Code,
    string ImageUrl,
    string MediaType,
    int? DurationSeconds,
    int DisplayOrder);
