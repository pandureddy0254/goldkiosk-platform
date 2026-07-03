namespace GoldKiosk.Cloud.Api.Services.Ai;

/// <summary>
/// An in-memory tray-camera image forwarded to the AI service. Tray/item frames only —
/// selfie/ID/PII imagery never reaches this type (image-separation rule).
/// </summary>
/// <param name="Content">The image bytes.</param>
/// <param name="FileName">The upload file name.</param>
/// <param name="ContentType">The MIME type, e.g. <c>image/jpeg</c>.</param>
public sealed record ItemImageUpload(ReadOnlyMemory<byte> Content, string FileName, string ContentType);
