using System.Globalization;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// Parses MT-SICS weight responses (e.g. <c>S S     12.345 g</c> stable,
/// <c>S D     12.401 g</c> dynamic). Replaces the legacy <c>Substring(3, 11)</c> +
/// machine-culture <c>Convert.ToDecimal</c> parse, which broke on short responses and on
/// comma-decimal locales: tokens are split on whitespace, the weight is the first numeric
/// token parsed with the invariant culture, and the stability flag comes from the status
/// token (<c>S</c> stable / <c>D</c> dynamic).
/// </summary>
public static class MtSicsWeightParser
{
    /// <summary>Attempts to parse one response line into a weight reading.</summary>
    /// <param name="response">The raw response line from the scale.</param>
    /// <param name="reading">The parsed reading (grams + stability) on success.</param>
    /// <returns><see langword="true"/> when a numeric weight token was found.</returns>
    public static bool TryParse(string? response, out WeightReading reading)
    {
        reading = new WeightReading(0m, Stable: false);
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        string[] tokens = response.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!decimal.TryParse(tokens[i], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal grams))
            {
                continue;
            }

            // MT-SICS marks a settling value with a 'D' (dynamic) status token before the
            // number; anything else (including the legacy 'Q' response with no marker) is
            // treated as settled.
            bool stable = true;
            for (int j = 0; j < i; j++)
            {
                if (string.Equals(tokens[j], "D", StringComparison.Ordinal))
                {
                    stable = false;
                    break;
                }
            }

            reading = new WeightReading(grams, stable);
            return true;
        }

        return false;
    }
}
