namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Optional physical-connection facts for one device, bound from
/// <c>Devices:Overrides:{deviceKey}:Connection</c>. Every member is nullable: a value that is
/// not configured falls back to the legacy-parity default from
/// <see cref="RealDeviceDefaults"/> via <see cref="MergedWith"/>. These are machine facts
/// (provisioning layer of the config pipeline) — never secrets.
/// </summary>
public sealed class ConnectionOptions
{
    /// <summary>Serial port name for RS-232 devices (e.g. <c>COM5</c> for the scale).</summary>
    public string? Port { get; set; }

    /// <summary>Serial baud rate (e.g. 9600 for the scale, 115200 for the pressure sensor).</summary>
    public int? BaudRate { get; set; }

    /// <summary>Network host for TCP/WebSocket devices (e.g. <c>192.168.1.6</c> for the Dobot arm).</summary>
    public string? Host { get; set; }

    /// <summary>Primary TCP port for network devices (e.g. 7860 for the Vanta WebSocket API).</summary>
    public int? TcpPort { get; set; }

    /// <summary>
    /// Path to the vendor SDK assembly for reflection-loaded drivers (Advantech, ARCA Envoy,
    /// Acuant, Gemalto, FlexCode, Camera_NET). Relative paths resolve against the application
    /// base directory. When the assembly is absent the device reports
    /// <c>Faulted</c> health with a <c>vendor_sdk_missing:*</c> detail instead of crashing.
    /// </summary>
    public string? VendorAssemblyPath { get; set; }

    /// <summary>
    /// Driver variant for ports served by more than one vendor SDK:
    /// metal analyser <c>vanta</c> | <c>innovx</c>, ID scanner <c>acuant</c> | <c>gemalto</c>.
    /// </summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Returns a copy of these options where every unset member is taken from
    /// <paramref name="fallback"/> (typically <see cref="RealDeviceDefaults.For"/>).
    /// </summary>
    /// <param name="fallback">The defaults supplying values for unset members.</param>
    /// <returns>The merged options; neither instance is mutated.</returns>
    public ConnectionOptions MergedWith(ConnectionOptions fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return new ConnectionOptions
        {
            Port = Port ?? fallback.Port,
            BaudRate = BaudRate ?? fallback.BaudRate,
            Host = Host ?? fallback.Host,
            TcpPort = TcpPort ?? fallback.TcpPort,
            VendorAssemblyPath = VendorAssemblyPath ?? fallback.VendorAssemblyPath,
            Variant = Variant ?? fallback.Variant,
        };
    }
}
