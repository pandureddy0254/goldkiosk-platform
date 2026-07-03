using System.Globalization;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// Parses one pressure sensor response line (command <c>P\r\n</c>, response like
/// <c>&gt;  4.9812 psi&lt;</c>). Ported from the legacy <c>PressureSensor.SamplePressure</c>
/// scrub (strip <c>\r</c>/<c>\n</c>/<c>&gt;</c>/<c>&lt;</c>, first space-separated token) but
/// with an invariant-culture parse instead of <c>Convert.ToDecimal</c>.
/// </summary>
public static class PressureLineParser
{
    /// <summary>Attempts to parse a pressure line into psi (gauge).</summary>
    /// <param name="line">The raw response line.</param>
    /// <param name="pressure">The parsed pressure on success.</param>
    /// <returns><see langword="true"/> when the line contained a numeric first token.</returns>
    public static bool TryParse(string? line, out decimal pressure)
    {
        pressure = 0m;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string cleaned = line.Trim()
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal)
            .Replace("<", string.Empty, StringComparison.Ordinal);
        string[] tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length > 0
            && decimal.TryParse(tokens[0], NumberStyles.Number, CultureInfo.InvariantCulture, out pressure);
    }
}
