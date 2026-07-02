using System.ComponentModel.DataAnnotations;
using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Options for the <c>Devices</c> configuration section (ADR 0004). Plain POCO — this
/// library has no framework references, so the Kiosk.Api host binds it:
/// <c>services.AddOptions&lt;DevicesOptions&gt;().BindConfiguration(DevicesOptions.SectionName)
/// .ValidateDataAnnotations().ValidateOnStart()</c>. Values resolve through the standard
/// config layering (configuration-and-operations §1): package defaults → machine
/// <c>kiosk-settings.json</c> → cloud static config (per-tenant + per-kiosk overlay, cached
/// with ETag) → environment (dev) — so a cloud-set mode overrides the local file.
/// </summary>
/// <remarks>
/// Nested members (<see cref="Overrides"/>, <see cref="Simulation"/>) are not validated by
/// <c>ValidateDataAnnotations()</c> (it does not recurse); <see cref="ResolveMode"/> fails
/// fast on invalid mode strings at composition time, and hosts should validate override keys
/// against <see cref="DeviceKeys.All"/> at startup.
/// </remarks>
public sealed class DevicesOptions
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "Devices";

    /// <summary>
    /// Fleet-default device mode: <c>"Real"</c> or <c>"Mock"</c> (case-insensitive).
    /// Production provisioning default is <c>Real</c>; Aspire dev default is <c>Mock</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^(?i)(Real|Mock)$", ErrorMessage = "Devices:DefaultMode must be 'Real' or 'Mock'.")]
    public string DefaultMode { get; set; } = "Real";

    /// <summary>
    /// Per-device mode overrides keyed by device key. Keys match <see cref="DeviceKeys"/>
    /// case- and underscore-insensitively, so both <c>"CashDispenser"</c> and
    /// <c>"cash_dispenser"</c> address the same device.
    /// </summary>
    public Dictionary<string, DeviceOverride> Overrides { get; init; } = [];

    /// <summary>Behaviour of the simulated devices (latency scaling, fault injection).</summary>
    public SimulationOptions Simulation { get; init; } = new();

    /// <summary>Resolves the effective mode for a device: its override when present, otherwise the default.</summary>
    /// <param name="key">The canonical device key (see <see cref="DeviceKeys"/>).</param>
    /// <returns>The effective <see cref="DeviceMode"/> for the device.</returns>
    /// <exception cref="InvalidOperationException">The configured mode string is not <c>Real</c> or <c>Mock</c>.</exception>
    public DeviceMode ResolveMode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string normalizedKey = Normalize(key);
        string configured = DefaultMode;
        foreach ((string overrideKey, DeviceOverride @override) in Overrides)
        {
            if (Normalize(overrideKey) == normalizedKey)
            {
                configured = @override.Mode;
                break;
            }
        }

        return Enum.TryParse(configured, ignoreCase: true, out DeviceMode mode)
            ? mode
            : throw new InvalidOperationException(
                $"Invalid device mode '{configured}' configured for '{key}' — expected 'Real' or 'Mock'.");
    }

    private static string Normalize(string key) =>
        key.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
