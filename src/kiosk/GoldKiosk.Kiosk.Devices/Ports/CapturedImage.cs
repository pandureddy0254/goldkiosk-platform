namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>A still image captured by the camera service.</summary>
/// <param name="Bytes">Encoded image bytes. Callers persist to the transaction folder; images are never logged.</param>
/// <param name="MediaType">IANA media type of <paramref name="Bytes"/> (e.g. <c>image/png</c>, <c>image/jpeg</c>).</param>
/// <param name="Role">The camera role that produced this capture.</param>
public sealed record CapturedImage(byte[] Bytes, string MediaType, CameraRole Role);
