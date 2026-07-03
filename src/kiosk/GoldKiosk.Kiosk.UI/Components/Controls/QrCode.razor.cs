using System.Globalization;
using System.Text;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>
/// Renders a payload as a QR symbol using the built-in encoder, drawn as a single SVG
/// path (with a quiet zone). Falls back to a mono text card when the payload exceeds
/// the encoder's capacity.
/// </summary>
public partial class QrCode
{
    private const int QuietZone = 4;

    private string? _path;
    private int _viewBox;
    private string? _encodedPayload;

    /// <summary>Gets or sets the payload to encode (e.g. the receipt URL).</summary>
    [Parameter]
    public string Payload { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_encodedPayload == Payload)
        {
            return;
        }

        _encodedPayload = Payload;
        _path = null;
        if (Payload.Length == 0 || !QrCodeGenerator.TryEncode(Payload, out bool[][] modules))
        {
            return;
        }

        int size = modules.Length;
        _viewBox = size + (2 * QuietZone);
        var path = new StringBuilder();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (modules[y][x])
                {
                    path.Append(
                        CultureInfo.InvariantCulture,
                        $"M{x + QuietZone},{y + QuietZone}h1v1h-1z");
                }
            }
        }

        _path = path.ToString();
    }
}
